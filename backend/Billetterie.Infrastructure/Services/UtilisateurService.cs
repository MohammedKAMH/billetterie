using Billetterie.Application.Interfaces;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Billetterie.Infrastructure.Services
{
    public class UtilisateurService : IUtilisateurService
    {
        private readonly BilletterieDbContext _context;

        public UtilisateurService(BilletterieDbContext context)
        {
            _context = context;
        }

        public async Task<Utilisateur?> GetUtilisateurAsync(string keycloakId)
        {
            return await _context.Utilisateurs
                .Include(u => u.Organisateur)
                .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
        }

        public async Task AddUtilisateurAsync(Utilisateur utilisateur)
        {
            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUtilisateurAsync(Utilisateur utilisateur)
        {
            _context.Utilisateurs.Update(utilisateur);
            await _context.SaveChangesAsync();
        }
    }
}
