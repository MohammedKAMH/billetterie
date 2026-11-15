using Billetterie.Domain.Entities;


namespace Billetterie.Application.Interfaces
{
    public interface IUtilisateurCourant
    {
        Task<Utilisateur?> GetAsync();
        Task<Guid> GetOrganisateurIdAsync();
    }
}
