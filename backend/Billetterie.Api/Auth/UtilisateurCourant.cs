using Billetterie.Api.Extensions;
using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;

namespace Billetterie.Api.Auth
{
    public interface IUtilisateurCourant
    {
        Task<Utilisateur?> GetAsync();

        // À n'appeler que derrière la policy OrganisateurValide.
        Task<Guid> GetOrganisateurIdAsync();
    }

    // Charge l'utilisateur du token une seule fois par requête.
    public class UtilisateurCourant : IUtilisateurCourant
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUtilisateurService _utilisateurService;
        private Utilisateur? _utilisateur;
        private bool _charge;

        public UtilisateurCourant(IHttpContextAccessor httpContextAccessor, IUtilisateurService utilisateurService)
        {
            _httpContextAccessor = httpContextAccessor;
            _utilisateurService = utilisateurService;
        }

        public async Task<Utilisateur?> GetAsync()
        {
            if (!_charge)
            {
                var keycloakId = _httpContextAccessor.HttpContext?.User.GetKeycloakId();
                _utilisateur = keycloakId is null ? null : await _utilisateurService.GetUtilisateurAsync(keycloakId);
                _charge = true;
            }

            return _utilisateur;
        }

        public async Task<Guid> GetOrganisateurIdAsync()
        {
            var utilisateur = await GetAsync();
            return utilisateur?.Organisateur?.Id
                ?? throw new InvalidOperationException("L'utilisateur courant n'est pas organisateur.");
        }
    }
}
