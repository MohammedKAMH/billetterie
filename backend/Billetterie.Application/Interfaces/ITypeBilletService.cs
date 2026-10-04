using Billetterie.Application.DTOs;
using Billetterie.Domain.Entities;


namespace Billetterie.Application.Interfaces
{
    public interface ITypeBilletService
    {
        Task AddTypeBilletAsync(TypeBillet typeBillet);
        Task<IList<TypeBillet>> GetByEvenementAsync(Guid evenementId);
        Task<TypeBillet?> GetTypeBilletByIdAsync(Guid id);
        Task UpdateTypeBilletAsync(TypeBillet typeBillet);
        Task DeleteTypeBilletAsync(TypeBillet typeBillet);



    }
}
