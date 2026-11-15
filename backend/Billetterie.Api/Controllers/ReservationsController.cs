using Billetterie.Api.Auth;
using Billetterie.Application.DTOs;
using Billetterie.Application.Interfaces;
using Billetterie.Application.ResultsEnum;
using Billetterie.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Billetterie.Api.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly IUtilisateurCourant _utilisateurCourant;

        public ReservationsController(IReservationService reservationService, IUtilisateurCourant utilisateurCourant)
        {
            _reservationService = reservationService;
            _utilisateurCourant = utilisateurCourant;

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateReservation(CreationReservationDto form)
        {
            var utilisateur = await _utilisateurCourant.GetAsync();

            if (utilisateur is null)
            {
                return BadRequest("Utilisateur inconnu");
            }

            var lignes = form.Lignes
                .Select(l => new LigneDemande(l.TypeBilletId, l.Quantite))
                .ToList();

            var resultat = await _reservationService.CreerReservationAsync(utilisateur.Id, lignes);

            if (!resultat.Succes)
            {
                return VersReponseEchec(resultat.Motif);
            }

            var reservation = resultat.Reservation!;

            var dto = VersDto(reservation);
            return Created(string.Empty, dto);
        }

        [Authorize]
        [HttpPost("{id}/payer")]
        public async Task<IActionResult> Payer(Guid id)
        {
            var utilisateur = await _utilisateurCourant.GetAsync();
            if (utilisateur is null)
            {
                return BadRequest("Utilisateur inconnu");
            }

            var resultat = await _reservationService.PayerReservationAsync(utilisateur.Id, id);
            if (!resultat.Succes)
            {
                return VersReponseEchec(resultat.Motif);
            }
            var reservation = resultat.Reservation!;
            var dto = VersDto(reservation);
            return Ok(dto);

        }

        [Authorize]
        [HttpPost("{id}/annuler")]
        public async Task<IActionResult> Annuler(Guid id)
        {
            var utilisateur = await _utilisateurCourant.GetAsync();
            if (utilisateur is null)
            {
                return BadRequest("Utilisateur inconnu");
            }

            var resultat = await _reservationService.AnnulerReservationAsync(utilisateur.Id, id);
            if (!resultat.Succes)
            {
                return VersReponseEchec(resultat.Motif);
            }
            var reservation = resultat.Reservation!;
            var dto = VersDto(reservation);
            return Ok(dto);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetReservations()
        {
            var utilisateur = await _utilisateurCourant.GetAsync();
            if (utilisateur is null)
            {
                return BadRequest("Utilisateur inconnu");
            }

            var reservations = await _reservationService.GetReservationsAsync(utilisateur.Id);

            return Ok(reservations.Select(VersDto));
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservationById(Guid id)
        {
            var utilisateur = await _utilisateurCourant.GetAsync();
            if (utilisateur is null)
            {
                return BadRequest("Utilisateur inconnu");
            }

            var reservation = await _reservationService.GetReservationByIdAsync(utilisateur.Id, id);

            if(reservation is null)
            {
                return NotFound();
            }
            
            return Ok(VersDto(reservation));
        }



        private IActionResult VersReponseEchec(MotifEchecEnumReservation? motif)
        {
            return motif switch
            {
                MotifEchecEnumReservation.TypeBilletIntrouvable
                    or MotifEchecEnumReservation.EvenementNonPublie => NotFound(),

                MotifEchecEnumReservation.TypesBilletDEvenementsDifferents
                    or MotifEchecEnumReservation.LigneEnDouble => BadRequest("Demande invalide."),

                MotifEchecEnumReservation.EvenementDejaCommence => Conflict("L'évènement a déjà commencé."),
                MotifEchecEnumReservation.StockInsuffisant => Conflict("Stock insuffisant."),
                MotifEchecEnumReservation.ReservationIntrouvable => NotFound(),
                MotifEchecEnumReservation.ReservationNonPayable
                    or MotifEchecEnumReservation.ReservationExpiree => Conflict("Cette réservation ne peut plus être payée."),
                MotifEchecEnumReservation.ReservationNonAnnulable => Conflict("Cette réservation ne peut plus être annulée."),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        private static ReservationDto VersDto(Reservation reservation) => new()
        {
            Id = reservation.Id,
            MontantTotal = reservation.MontantTotal,
            DateExpiration = reservation.DateExpiration,
            StatutPaiement = reservation.StatutPaiement,
            Billets = reservation.Billets.Select(b => new BilletDto
            {
                Id = b.Id,
                NomTypeBillet = b.TypeBillet?.Nom ?? string.Empty,
                Statut = b.Statut,
                CodeUnique = reservation.StatutPaiement == StatutPaiement.Paye ? b.CodeUnique : null
            }).ToList()
        };





    }
}
