using System.Buffers.Text;
using System.Security.Cryptography;
using Billetterie.Application.Interfaces;
using Billetterie.Application.ResultsEnum;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
                return ResultatReservation.Echec(MotifEchecEnum.LigneEnDouble);
            }

            var typesBillets = await _context.TypeBillets
                .Include(t => t.Evenement)
                .Where(t => ids.Contains(t.Id))
                .ToListAsync();

            if (typesBillets.Count != ids.Count)
            {
                return ResultatReservation.Echec(MotifEchecEnum.TypeBilletIntrouvable);
            }

            if (typesBillets.Select(t => t.EvenementId).Distinct().Count() != 1)
            {
                return ResultatReservation.Echec(MotifEchecEnum.TypesBilletDEvenementsDifferents);
            }

            var evenement = typesBillets[0].Evenement;
            if (evenement.Statut != EvenementStatut.Publie)
            {
                return ResultatReservation.Echec(MotifEchecEnum.EvenementNonPublie);
            }

            if (evenement.DateDebut <= maintenant)
            {
                return ResultatReservation.Echec(MotifEchecEnum.EvenementDejaCommence);
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
                    return ResultatReservation.Echec(MotifEchecEnum.StockInsuffisant);
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

        private static string GenererCode()
            => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
    }
}