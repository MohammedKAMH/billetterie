using Billetterie.Api.Auth;
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
        private readonly IUtilisateurCourant _utilisateurCourant;
        private readonly ITypeBilletService _typeBilletService;

        public EvenementsController(IEvenementService evenementService,
            IUtilisateurCourant utilisateurCourant,
            ITypeBilletService typeBilletService)
        {
            _evenementService = evenementService;
            _utilisateurCourant = utilisateurCourant;
            _typeBilletService = typeBilletService;
        }

        [Authorize(Policy = Policies.OrganisateurValide)]
        [HttpPost]
        public async Task<IActionResult> CreateEvenement(CreationEvenementDto evenementForm)
        {
            var evenement = new Evenement
            {
                Id = Guid.NewGuid(),
                OrganisateurId = await _utilisateurCourant.GetOrganisateurIdAsync(),
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

        [Authorize(Policy = Policies.OrganisateurValide)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvenement(Guid id, UpdateEvenementDto update)
        {
            var evenement = await _evenementService.GetEvenementByIdAsync(id);
            if (evenement is null)
            {
                return NotFound();
            }

            if (evenement.OrganisateurId != await _utilisateurCourant.GetOrganisateurIdAsync())
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

        [Authorize(Policy = Policies.OrganisateurValide)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> AnnulerEvenement(Guid id)
        {
            var evenement = await _evenementService.GetEvenementByIdAsync(id);
            if(evenement is null)
            {
                return NotFound();
            }

            if (evenement.OrganisateurId != await _utilisateurCourant.GetOrganisateurIdAsync())
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

        [Authorize(Policy = Policies.OrganisateurValide)]
        [HttpPost("{id}/publier")]
        public async Task<IActionResult> PublierEvenement(Guid id)
        {
            var evenement = await _evenementService.GetEvenementByIdAsync(id);
            if (evenement is null)
            {
                return NotFound();
            }

            if (evenement.OrganisateurId != await _utilisateurCourant.GetOrganisateurIdAsync())
            {
                return Forbid();
            }

            if (evenement.Statut != EvenementStatut.Brouillon)
            {
                return Conflict("Cet évènement ne peut plus être publié.");
            }

            var typebillets = await _typeBilletService.GetByEvenementAsync(evenement.Id);
            if (typebillets.Count == 0)
            {
                return Conflict("Il faut au moins un type de billet pour publier cet évènement.");
            }

            await _evenementService.PublierEvenementAsync(evenement);

            return NoContent();
        }

    }



}