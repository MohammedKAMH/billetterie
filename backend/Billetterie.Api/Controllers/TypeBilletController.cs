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

        [Authorize(Roles = "Organisateur")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTypeBillet(Guid evenementId, Guid id, UpdateTypeBilletDto updated)
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
                return Conflict("Impossible de modifier un type de billet de cet évènement.");
            }

            var typeBillet = await _typeBilletService.GetTypeBilletByIdAsync(id);
            if (typeBillet is null || typeBillet.EvenementId != evenementId)
            {
                return NotFound();
            }

            if (updated.QuantiteTotale < typeBillet.QuantiteVendue)
            {
                return Conflict("La quantité totale ne peut pas être inférieure au nombre de billets déjà vendus.");
            }

            if (typeBillet.QuantiteVendue > 0 && updated.Prix != typeBillet.Prix)
            {
                return Conflict("Le prix ne peut plus être modifié : des billets ont déjà été vendus.");
            }

            typeBillet.Nom = updated.Nom;
            typeBillet.Prix = updated.Prix;
            typeBillet.QuantiteTotale = updated.QuantiteTotale;

            await _typeBilletService.UpdateTypeBilletAsync(typeBillet);

            return Ok(new { typeBillet.Id, typeBillet.Nom, typeBillet.Prix, typeBillet.QuantiteTotale });
        }

        [Authorize(Roles = "Organisateur")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTypeBillet(Guid evenementId, Guid id)
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

            var typeBillet = await _typeBilletService.GetTypeBilletByIdAsync(id);
            if (typeBillet is null || typeBillet.EvenementId != evenementId)
            {
                return NotFound();
            }

            if (typeBillet.QuantiteVendue > 0)
            {
                return Conflict("Impossible de supprimer un type de billet dont des billets ont déjà été vendus.");
            }

            await _typeBilletService.DeleteTypeBilletAsync(typeBillet);

            return NoContent();
        }


    }


}
