using Billetterie.Api.Extensions;
using Billetterie.Application.DTOs;
using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Billetterie.Api.Controllers
{
    [ApiController]
    [Route("api/evenements/{evenementId}/types-billet")]
    public class TypeBilletController : ControllerBase
    {
        private readonly ITypeBilletService _typeBilletService;
        private readonly IEvenementService _evenementService;
        public readonly IUtilisateurService _utilisateurService;

        public TypeBilletController(
            ITypeBilletService typeBilletService,
            IEvenementService evenementService,
            IUtilisateurService utilisateurService)
        {
            _typeBilletService = typeBilletService;
            _evenementService = evenementService;
            _utilisateurService = utilisateurService;
        }

        [Authorize(Roles = "Organisateur")]
        [HttpPost]
        public async Task<IActionResult> CreateTypeBillet(Guid evenementId, CreationTypeBilletDto form)
        {
            var keycloakId = User.GetKeycloakId();
            if (keycloakId is null)
            {
                return BadRequest("Token invalide");
            }

            var utilisateur = await _utilisateurService.GetUtilisateurAsync(keycloakId);

            if (utilisateur?.Organisateur is null)
            {
                return Forbid();
            }

            var evenement = await _evenementService.GetEvenementByIdAsync(evenementId);
            if (evenement is null)
            {
                return NotFound();
            }

            if (evenement.OrganisateurId != utilisateur.Organisateur.Id)
            {
                return Forbid();
            }

            if (evenement.Statut is EvenementStatut.Annule or EvenementStatut.Termine)
            {
                return Conflict("Impossible d'ajouter un type de billet à cet évènement.");
            }

            var typeBillet = new TypeBillet
            {
                Id = Guid.NewGuid(),
                EvenementId = evenementId,
                Nom = form.Nom,
                Prix = form.Prix,
                QuantiteTotale = form.QuantiteTotale,
                QuantiteVendue = 0
            };

            await _typeBilletService.AddTypeBilletAsync(typeBillet);
            return Created(string.Empty, new { typeBillet.Id, typeBillet.Nom });
        }

        [HttpGet]
        public async Task<IActionResult> GetTypesBillet(Guid evenementId)
        {
            var evenement = await _evenementService.GetPublishedEvenementByIdAsync(evenementId);
            if(evenement is null)
            {
                return NotFound();
            }

            var typesBillet = await _typeBilletService.GetByEvenementAsync(evenementId);
            var dtos = typesBillet.Select(t => new TypeBilletDto
            {
                Id = t.Id,
                Nom = t.Nom,
                Prix = t.Prix,
                QuantiteDisponible = t.QuantiteDisponible
            });

            return Ok(dtos);
                
        }




    }


}
