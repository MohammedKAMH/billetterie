using Billetterie.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Billetterie.Api.Auth
{
    public static class Policies
    {
        public const string OrganisateurValide = "OrganisateurValide";
    }

    public class OrganisateurValideRequirement : IAuthorizationRequirement { }

    public class OrganisateurValideHandler : AuthorizationHandler<OrganisateurValideRequirement>
    {
        private readonly IUtilisateurCourant _utilisateurCourant;

        public OrganisateurValideHandler(IUtilisateurCourant utilisateurCourant)
        {
            _utilisateurCourant = utilisateurCourant;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OrganisateurValideRequirement requirement)
        {
            var utilisateur = await _utilisateurCourant.GetAsync();
            if (utilisateur?.Organisateur?.StatutValidation == StatutValidation.Valide)
            {
                context.Succeed(requirement);
            }
        }
    }
}
