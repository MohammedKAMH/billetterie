using Billetterie.Api.Extensions;
using Billetterie.Application.DTOs;
using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billetterie.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvenementsController : ControllerBase
    {
        private readonly IEvenementService _evenementService;
        private readonly IUtilisateurService _utilisateurService;

        public EvenementsController(IEvenementService evenementService, IUtilisateurService utilisateurService)
        {
            _evenementService = evenementService;
            _utilisateurService = utilisateurService;
        }

        [Authorize(Roles = "Organisateur")]
        [HttpPost]
        public async Task<IActionResult> CreateEvenement(CreationEvenementDto evenementForm)
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

            if (utilisateur.Organisateur.StatutValidation != StatutValidation.Valide)
            {
                return Forbid();
            }

            var evenement = new Evenement
            {
                Id = Guid.NewGuid(),
                OrganisateurId = utilisateur.Organisateur.Id,
                Titre = evenementForm.Titre,
                Lieu = evenementForm.Lieu,
                Description = evenementForm.Description,
                DateDebut = evenementForm.DateDebut,
                DateFin = evenementForm.DateFin,
                Statut = EvenementStatut.Brouillon
            };

            await _evenementService.AddEvenementAsync(evenement);

            return Created(string.Empty, new { evenement.Id, evenement.Titre, statut = evenement.Statut });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvenementById(Guid id)
        {
            var evenement = await _evenementService.GetEvenementByIdAsync(id);
            if (evenement is null)
            {
                return NotFound();
            }

            var dto = new EvenementDto
            {
                Id = evenement.Id,
                Titre = evenement.Titre,
                Description = evenement.Description,
                Lieu = evenement.Lieu,
                DateDebut = evenement.DateDebut,
                DateFin = evenement.DateFin,
                NomOrganisateur = $"{evenement.Organisateur.Utilisateur.Prenom} {evenement.Organisateur.Utilisateur.Nom}"
            };

            return Ok(dto);

        }

        [HttpGet]
        public async Task<IActionResult> GetAllEvenements()
        {
            var evenements = await _evenementService.GetPublishedEvenementsAsync();
            var dtos = evenements.Select(e => new EvenementDto
            {
                Id = e.Id,
                Titre = e.Titre,
                Description = e.Description,
                Lieu = e.Lieu,
                DateDebut = e.DateDebut,
                DateFin = e.DateFin,
                NomOrganisateur = $"{e.Organisateur.Utilisateur.Prenom} {e.Organisateur.Utilisateur.Nom}"
            });
            return Ok(dtos);
        }

        [Authorize(Roles = "Organisateur")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvenement(Guid id, UpdateEvenementDto update)
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

            var evenement = await _evenementService.GetEvenementByIdAsync(id);
            if (evenement is null)
            {
                return NotFound();
            }

            if (evenement.OrganisateurId != utilisateur.Organisateur.Id)
            {
                return Forbid();
            }

            evenement.Titre = update.Titre;
            evenement.Description = update.Description;
            evenement.Lieu = update.Lieu;
            evenement.DateDebut = update.DateDebut;
            evenement.DateFin = update.DateFin;

            await _evenementService.UpdateEvenementAsync(evenement);

            return Ok(new { evenement.Id, evenement.Titre, statut = evenement.Statut });
        }

        [Authorize(Roles = "Organisateur")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> AnnulerEvenementAsync(Guid id)
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

            if (utilisateur.Organisateur.StatutValidation != StatutValidation.Valide)
            {
                return Forbid();
            }
            var evenement = await _evenementService.GetEvenementByIdAsync(id);
            if(evenement is null)
            {
                return NotFound();
            }

            if (evenement.OrganisateurId != utilisateur.Organisateur.Id)
            {
                return Forbid();
            }

            if (evenement.Statut is EvenementStatut.Annule or EvenementStatut.Termine)
            {
                return Conflict("Cet évènement ne peut plus être annulé.");
            }

            await _evenementService.AnnulerEvenementAsync(evenement);

            return NoContent();
        }
    }


}