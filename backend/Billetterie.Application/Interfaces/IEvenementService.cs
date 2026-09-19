using Billetterie.Domain.Entities;


namespace Billetterie.Application.Interfaces
{
    public interface IEvenementService
    {
        Task AddEvenementAsync(Evenement evenement);

    }
}
