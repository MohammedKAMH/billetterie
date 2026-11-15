

using Billetterie.Application.DTOs;
using Billetterie.Application.ResultsEnum;
using Billetterie.Domain.Entities;

namespace Billetterie.Application.Interfaces
{
    public interface IOrganisateurService
    {
        Task<ResultatOrganisateur> DemanderAsync(Guid utilisateurId);
        Task<Organisateur?> GetParUtilisateurAsync(Guid utilisateurId);
        Task<IList<Organisateur>> GetDemandesAsync(StatutValidation? statut);
        Task<ResultatOrganisateur> ValiderAsync(Guid organisateurId, Guid adminId);
        Task<ResultatOrganisateur> RefuserAsync(Guid organisateurId, Guid adminId);


    }
}
