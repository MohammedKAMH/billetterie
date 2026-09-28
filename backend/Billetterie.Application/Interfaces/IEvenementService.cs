using Billetterie.Application.DTOs;
using Billetterie.Domain.Entities;


namespace Billetterie.Application.Interfaces
{
    public interface IEvenementService
    {
        Task AddEvenementAsync(Evenement evenement);

        Task<Evenement?> GetEvenementByIdAsync(Guid id);

        Task<Evenement?> GetPublishedEvenementByIdAsync(Guid id);
        Task<IList<Evenement>> GetPublishedEvenementsAsync();

        Task UpdateEvenementAsync(Evenement evenement);
        Task AnnulerEvenementAsync(Evenement evenement);
    }
}
