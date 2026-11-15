using Billetterie.Application.Interfaces;
using Billetterie.Application.ResultsEnum;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Buffers.Text;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;

namespace Billetterie.Infrastructure.Services
{
    public class ReservationService : IReservationService
    {
        private readonly BilletterieDbContext _context;
        private readonly TimeProvider _timeProvider;
        private readonly IConfiguration _configuration;

        public ReservationService( BilletterieDbContext context, TimeProvider timeProvider, IConfiguration configuration)
        {
            _context = context;
            _timeProvider = timeProvider;
            _configuration = configuration;
        }

        public async Task<ResultatReservation> CreerReservationAsync( Guid utilisateurId, IReadOnlyList<LigneDemande> lignes)
        {
            var maintenant = _timeProvider.GetUtcNow().UtcDateTime;

            var ids = lignes.Select(l => l.TypeBilletId).ToList();
            if (ids.Distinct().Count() != ids.Count)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.LigneEnDouble);
            }

            var typesBillets = await _context.TypeBillets
                .Include(t => t.Evenement)
                .Where(t => ids.Contains(t.Id))
                .ToListAsync();

            if (typesBillets.Count != ids.Count)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.TypeBilletIntrouvable);
            }

            if (typesBillets.Select(t => t.EvenementId).Distinct().Count() != 1)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.TypesBilletDEvenementsDifferents);
            }

            var evenement = typesBillets[0].Evenement;
            if (evenement.Statut != EvenementStatut.Publie)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.EvenementNonPublie);
            }

            if (evenement.DateDebut <= maintenant)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.EvenementDejaCommence);
            }

            var typesParId = typesBillets.ToDictionary(t => t.Id);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var ligne in lignes)
            {
                var lignesAffectees = await _context.TypeBillets
                    .Where(t => t.Id == ligne.TypeBilletId
                             && t.QuantiteVendue + ligne.Quantite <= t.QuantiteTotale)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        t => t.QuantiteVendue,
                        t => t.QuantiteVendue + ligne.Quantite));

                if (lignesAffectees == 0)
                {
                    return ResultatReservation.Echec(MotifEchecEnumReservation.StockInsuffisant);
                }
            }

            var montant = lignes.Sum(l => typesParId[l.TypeBilletId].Prix * l.Quantite);

            var dureeMinutes = _configuration.GetValue("Reservation:DureeExpirationMinutes", 15);

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                UtilisateurId = utilisateurId,
                DateReservation = maintenant,
                DateExpiration = maintenant.AddMinutes(dureeMinutes),
                StatutPaiement = StatutPaiement.EnAttente,
                MontantTotal = montant
            };

            foreach (var ligne in lignes)
            {
                for (var i = 0; i < ligne.Quantite; i++)
                {
                    reservation.Billets.Add(new Billet
                    {
                        Id = Guid.NewGuid(),
                        TypeBilletId = ligne.TypeBilletId,
                        Statut = BilletStatut.Valide,
                        CodeUnique = GenererCode()
                    });
                }
            }

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ResultatReservation.Reussi(reservation);
        }

        public async Task<ResultatReservation> PayerReservationAsync(Guid utilisateurId, Guid reservationId)
        {
            var maintenant = _timeProvider.GetUtcNow().UtcDateTime;
            var reservation = await _context.Reservations
                .Include(r => r.Billets)
                    .ThenInclude(b => b.TypeBillet)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UtilisateurId == utilisateurId);
            
            if(reservation == null)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.ReservationIntrouvable);
            }

            if(reservation.StatutPaiement != StatutPaiement.EnAttente)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.ReservationNonPayable);
            }

            if(reservation.DateExpiration <= maintenant)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.ReservationExpiree);
            }

            var lignesAffectees = await _context.Reservations
                .Where(r => r.Id == reservationId
                         && r.StatutPaiement == StatutPaiement.EnAttente
                         && r.DateExpiration > maintenant)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    r => r.StatutPaiement,
                    StatutPaiement.Paye));

            if (lignesAffectees == 0)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.ReservationNonPayable);
            }

            reservation.StatutPaiement = StatutPaiement.Paye;
            return ResultatReservation.Reussi(reservation);

        }

        public async Task<ResultatReservation> AnnulerReservationAsync(Guid utilisateurId, Guid reservationId){
            var reservation = await _context.Reservations
                .Include(r => r.Billets)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UtilisateurId == utilisateurId);

            if(reservation is null)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.ReservationIntrouvable);
            }

            if(reservation.StatutPaiement != StatutPaiement.EnAttente)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.ReservationNonAnnulable);
            }

            var libere = await LibererAsync(reservationId, StatutPaiement.Annule);

            if(!libere)
            {
                return ResultatReservation.Echec(MotifEchecEnumReservation.ReservationNonAnnulable);
            }

            reservation.StatutPaiement = StatutPaiement.Annule;

            foreach(var billet in reservation.Billets)
            {
                billet.Statut = BilletStatut.Annule;
            }

            return ResultatReservation.Reussi(reservation);
        }


        public async Task<IList<Reservation>> GetReservationsAsync(Guid utilisateurId)
        {
            return await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Billets)
                    .ThenInclude(b => b.TypeBillet)
                .Where(r => r.UtilisateurId == utilisateurId)
                .OrderByDescending(r => r.DateReservation)
                .ToListAsync();
               
        }
        public async Task<Reservation?> GetReservationByIdAsync(Guid utilisateurId, Guid reservationId)
        {
            return await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Billets)
                    .ThenInclude(b => b.TypeBillet)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UtilisateurId == utilisateurId);
        }

        public async Task<int> ExpirerReservationsAsync(CancellationToken ct = default)
        {
            var maintenant = _timeProvider.GetUtcNow().UtcDateTime;
            var cpt = 0;

            var ids = await _context.Reservations
                .Where(r => r.StatutPaiement == StatutPaiement.EnAttente && r.DateExpiration <= maintenant)
                .Select(r => r.Id)
                .Take(100)
                .ToListAsync(ct);

            foreach(var id in ids)
            {
                ct.ThrowIfCancellationRequested();
                var libere = await LibererAsync(id, StatutPaiement.Expire);
                if (libere) cpt++;
            }
            return cpt;
        }



        private static string GenererCode()
            => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));

        private async Task<bool> LibererAsync(Guid reservationId, StatutPaiement nouveauStatut)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var lignesAffectees = await _context.Reservations
                    .Where(r => r.Id == reservationId
                             && r.StatutPaiement == StatutPaiement.EnAttente)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        r => r.StatutPaiement,
                        nouveauStatut));

            if (lignesAffectees == 0) return false;

            await _context.Billets
                    .Where(b => b.ReservationId == reservationId)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        b => b.Statut,
                        BilletStatut.Annule));

            var parType = await _context.Billets
                .Where(b => b.ReservationId == reservationId)
                .GroupBy(b => b.TypeBilletId)
                .Select(g => new { TypeBilletId = g.Key, Nombre = g.Count() })
                .ToListAsync();

            foreach (var groupe in parType)
            {
                await _context.TypeBillets
                    .Where(t => t.Id == groupe.TypeBilletId)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        t => t.QuantiteVendue,
                        t => t.QuantiteVendue - groupe.Nombre));
            }
            await transaction.CommitAsync();
            return true;
        }
    }

}