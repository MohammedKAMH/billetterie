using Billetterie.Application.DTOs;
using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Billetterie.Infrastructure.Services
{
    public class TypeBilletService : ITypeBilletService
    {
        private readonly BilletterieDbContext _context;
        public TypeBilletService(BilletterieDbContext context)
        {
            _context = context;
        }
        public async Task AddTypeBilletAsync(TypeBillet typeBillet)
        {
            _context.TypeBillets.Add(typeBillet);
            await _context.SaveChangesAsync();
        }

        public async Task<IList<TypeBillet>> GetByEvenementAsync( Guid evenementId)
        {
            return await _context.TypeBillets
                .Where(t => t.EvenementId == evenementId)
                .ToListAsync();
        }
    }
}
