using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

        public async Task<Evenement?> GetEvenementByIdAsync(Guid id)
        {
            return await _context.Evenements
                .FirstOrDefaultAsync(e => e.Id == id);
        }
        public async Task<Evenement?> GetPublishedEvenementByIdAsync(Guid id)
        {
            return await _context.Evenements
                .Include(e => e.Organisateur)
                        .ThenInclude(o => o.Utilisateur)
                    .Where(e => e.Statut == EvenementStatut.Publie)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IList<Evenement>?> GetPublishedEvenementsAsync()
        {
            return await _context.Evenements
                .Include(e => e.Organisateur)
                    .ThenInclude(o => o.Utilisateur)
                .Where(e => e.Statut == EvenementStatut.Publie)
                .ToListAsync();

        }

        public async Task UpdateEvenementAsync(Evenement evenement)
        {
            _context.Evenements.Update(evenement);
            await _context.SaveChangesAsync();
        }

        public async Task AnnulerEvenementAsync(Evenement evenement)
        {
            evenement.Statut = EvenementStatut.Annule;
            _context.Evenements.Update(evenement);
            await _context.SaveChangesAsync();
        }
    }
}
