using Billetterie.Api.Auth;
using Billetterie.Application.DTOs;
using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganisateursController : ControllerBase
{
    private readonly IOrganisateurService _organisateurService;
    private readonly IUtilisateurCourant _utilisateurCourant;

    public OrganisateursController(IOrganisateurService organisateurService, IUtilisateurCourant utilisateurCourant)
    {
        _organisateurService = organisateurService;
        _utilisateurCourant = utilisateurCourant;
    }

    [HttpPost("demande")]
    public async Task<IActionResult> Demande()
    {
        var utilisateur = await _utilisateurCourant.GetAsync();
        if (utilisateur is null)
        {
            return Conflict("Synchronise ton compte d'abord (POST /api/utilisateurs/sync).");
        }

        var resultat = await _organisateurService.DemanderAsync(utilisateur.Id);
        if (!resultat.Succes)
        {
            return Conflict("Une demande existe déjà pour cet utilisateur.");
        }

        return Created(string.Empty, VersDto(resultat.Organisateur!));
    }

    [HttpGet("ma-demande")]
    public async Task<IActionResult> MaDemande()
    {
        var utilisateur = await _utilisateurCourant.GetAsync();
        if (utilisateur is null)
        {
            return Conflict("Synchronise ton compte d'abord (POST /api/utilisateurs/sync).");
        }

        var organisateur = await _organisateurService.GetParUtilisateurAsync(utilisateur.Id);
        if (organisateur is null)
        {
            return NotFound();
        }

        return Ok(VersDto(organisateur));
    }

    private static DemandeOrganisateurDto VersDto(Organisateur o) => new()
    {
        StatutValidation = o.StatutValidation,
        DateValidation = o.DateValidation
    };
}