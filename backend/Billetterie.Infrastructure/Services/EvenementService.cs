using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Data;

namespace Billetterie.Infrastructure.Services
{
    public class EvenementService : IEvenementService
    {
        private readonly BilletterieDbContext _context;

        public EvenementService(BilletterieDbContext context)
        {
            _context = context;
        }
        public async Task AddEvenementAsync(Evenement evenement)
        {
            _context.Evenements.Add(evenement);
            await _context.SaveChangesAsync();
        }
    }
}
