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
    [Route("api/reservation")]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;
        private readonly IUtilisateurCourant _utilisateurCourant;

        public ReservationController(IReservationService reservationService, IUtilisateurCourant utilisateurCourant)
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
                return BadRequest("Utilisateur inconnu : appelez d'abord POST /api/utilisateurs/sync.");
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

            var dto = new ReservationDto
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
                    CodeUnique = reservation.StatutPaiement == StatutPaiement.Paye
                        ? b.CodeUnique
                        : null
                }).ToList()
            };

            return Created(string.Empty, dto);
        }


        private IActionResult VersReponseEchec(MotifEchecEnum? motif)
        {
            return motif switch
            {
                MotifEchecEnum.TypeBilletIntrouvable
                    or MotifEchecEnum.EvenementNonPublie => NotFound(),

                MotifEchecEnum.TypesBilletDEvenementsDifferents
                    or MotifEchecEnum.LigneEnDouble => BadRequest("Demande invalide."),

                MotifEchecEnum.EvenementDejaCommence => Conflict("L'évènement a déjà commencé."),
                MotifEchecEnum.StockInsuffisant => Conflict("Stock insuffisant."),

                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }





    }
}
