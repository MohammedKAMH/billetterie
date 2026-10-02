using Billetterie.Application.DTOs;
using Billetterie.Domain.Entities;


namespace Billetterie.Application.Interfaces
{
    public interface ITypeBilletService
    {
        Task AddTypeBilletAsync(TypeBillet typeBillet);
        Task<IList<TypeBillet>> GetByEvenementAsync(Guid evenementId);

    }
}
