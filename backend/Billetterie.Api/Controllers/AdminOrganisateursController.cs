using Billetterie.Api.Auth;
using Billetterie.Application.DTOs;
using Billetterie.Application.Interfaces;
using Billetterie.Application.ResultsEnum;
using Billetterie.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/organisateurs")]
[Authorize(Policy = Policies.Admin)]
public class AdminOrganisateursController : ControllerBase
{
    private readonly IOrganisateurService _organisateurService;
    private readonly IUtilisateurCourant _utilisateurCourant;

    public AdminOrganisateursController(IOrganisateurService organisateurService, IUtilisateurCourant utilisateurCourant)
    {
        _organisateurService = organisateurService;
        _utilisateurCourant = utilisateurCourant;
    }

    [HttpGet]
    public async Task<IActionResult> GetDemandes([FromQuery] StatutValidation? statut)
    {
        var demandes = await _organisateurService.GetDemandesAsync(statut);

        var dtos = demandes.Select(o => new DemandeAdminDto
        {
            Id = o.Id,
            Email = o.Utilisateur.Email,
            Nom = o.Utilisateur.Nom,
            Prenom = o.Utilisateur.Prenom,
            StatutValidation = o.StatutValidation,
            DateValidation = o.DateValidation
        });

        return Ok(dtos);
    }

    [HttpPost("{id}/valider")]
    public async Task<IActionResult> valider(Guid id)
    {
        var admin =await  _utilisateurCourant.GetAsync();
     
        if (admin is null)
        {
            return Conflict("Synchronise ton compte d'abord (POST /api/utilisateurs/sync).");
        }
        var resultat = await _organisateurService.ValiderAsync(id, admin.Id);

        return VersReponse(resultat);
    }


    [HttpPost("{id}/refuser")]
    public async Task<IActionResult> refuser (Guid id)
    {
        var admin =await  _utilisateurCourant.GetAsync();

        if (admin is null)
        {
            return Conflict("Synchronise ton compte d'abord (POST /api/utilisateurs/sync).");
        }

        var resultat = await _organisateurService.RefuserAsync(id, admin.Id);
        return VersReponse(resultat);
        
    }

    private IActionResult VersReponse(ResultatOrganisateur resultat)
    {
        if (resultat.Succes)
        {
            return NoContent();
        }

        return resultat.Motif switch
        {
            MotifEchecOrganisateurEnum.DemandeIntrouvable => NotFound(),
            MotifEchecOrganisateurEnum.DemandeNonEnAttente => Conflict("Cette demande a déjà été traitée."),
            _ => Conflict("Opération impossible.")
        };
    }
}