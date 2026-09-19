using Billetterie.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Billetterie.Application.Interfaces;

namespace Billetterie.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UtilisateursController : ControllerBase
    {
        private readonly IUtilisateurService _utilisateurService;

        public UtilisateursController(IUtilisateurService utilisateurService)
        {
            _utilisateurService = utilisateurService;
        }

        [Authorize]
        [HttpPost("sync")]
        public async Task<IActionResult> Sync()
        {
            var keycloakId = User.FindFirst("sub")?.Value;
            var email = User.FindFirst("email")?.Value;
            var nom = User.FindFirst("family_name")?.Value;
            var prenom = User.FindFirst("given_name")?.Value;

            if (string.IsNullOrEmpty(keycloakId) || string.IsNullOrEmpty(email))
            {
                return BadRequest("Token incomplet : KeycloakId ou email manquant.");
            }

            var utilisateur = await _utilisateurService.GetUtilisateurAsync(keycloakId);

            if (utilisateur is null)
            {
                utilisateur = new Utilisateur
                {
                    Id = Guid.NewGuid(),
                    KeycloakId = keycloakId,
                    Email = email,
                    Nom = nom ?? string.Empty,
                    Prenom = prenom ?? string.Empty
                };

                await _utilisateurService.AddUtilisateurAsync(utilisateur); 

                return Ok(new {utilisateur.Id, utilisateur.Email, status = "existant" });
            }

            utilisateur.Email = email;
            utilisateur.Nom = nom ?? utilisateur.Nom;
            utilisateur.Prenom = prenom ?? utilisateur.Prenom;

            await _utilisateurService.UpdateUtilisateurAsync(utilisateur);

            return Ok(new { utilisateur.Id, utilisateur.Email, statut = "synchronise" });
        }
    }
}