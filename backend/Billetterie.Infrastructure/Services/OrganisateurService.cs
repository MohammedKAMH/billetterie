using Billetterie.Application.DTOs;
using Billetterie.Application.Interfaces;
using Billetterie.Application.ResultsEnum;
using Billetterie.Domain.Entities;
using Billetterie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Infrastructure.Services
{
    public class OrganisateurService : IOrganisateurService
    {
        private readonly BilletterieDbContext _context;
        private readonly TimeProvider _timeProvider;
        public OrganisateurService(BilletterieDbContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<ResultatOrganisateur> DemanderAsync(Guid utilisateurId)
        {
            var existe =await  _context.Organisateurs.AnyAsync(o => o.UtilisateurId == utilisateurId);
            
            if (existe)
            {
                return ResultatOrganisateur.Echec(MotifEchecOrganisateurEnum.DemandeDejaExistante);
            }

            var organisateur = new Organisateur
            {
                Id = Guid.NewGuid(),
                UtilisateurId = utilisateurId,
                StatutValidation = StatutValidation.EnAttente
            };

            _context.Organisateurs.Add(organisateur);

            try {
                await _context.SaveChangesAsync(); }
            catch (DbUpdateException) {
                return ResultatOrganisateur.Echec(MotifEchecOrganisateurEnum.DemandeDejaExistante);
            }

            return ResultatOrganisateur.Reussi(organisateur);
        }
        public async Task<Organisateur?> GetParUtilisateurAsync(Guid utilisateurId)
        {
            return await _context.Organisateurs
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UtilisateurId == utilisateurId);
        }

        public async Task<IList<Organisateur>> GetDemandesAsync(StatutValidation? statut)
        {
            var requete = _context.Organisateurs
                .AsNoTracking()
                .Include(o => o.Utilisateur)
                .AsQueryable();
            
            if(statut is not null)
            {
                requete = requete.Where(o => o.StatutValidation == statut);
            }
            return await requete
               .OrderBy(o => o.Utilisateur.Email)
               .ToListAsync();
        }
        public  Task<ResultatOrganisateur> ValiderAsync(Guid organisateurId, Guid adminId)
            => ChangerStatutAsync(organisateurId, adminId, StatutValidation.Valide);

        public Task<ResultatOrganisateur> RefuserAsync(Guid organisateurId, Guid adminId)
            => ChangerStatutAsync(organisateurId, adminId, StatutValidation.Refuse);


        private async Task<ResultatOrganisateur> ChangerStatutAsync(
            Guid organisateurId, Guid adminId, StatutValidation nouveauStatut)
        {
            var maintenant = _timeProvider.GetUtcNow().UtcDateTime;

            var estSaPropreDemande = await _context.Organisateurs
                .AnyAsync(o => o.Id == organisateurId && o.UtilisateurId == adminId);

            if (estSaPropreDemande)
            {
                return ResultatOrganisateur.Echec(MotifEchecOrganisateurEnum.ActionSurSaPropreDemande);
            }

            var lignesAffectees = await _context.Organisateurs
                .Where(o => o.Id == organisateurId
                         && o.StatutValidation == StatutValidation.EnAttente)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.StatutValidation, nouveauStatut)
                    .SetProperty(o => o.ValideParUtilisateurId, adminId)
                    .SetProperty(o => o.DateValidation, maintenant));

            if (lignesAffectees == 0)
            {
                var existe = await _context.Organisateurs.AnyAsync(o => o.Id == organisateurId);
                return ResultatOrganisateur.Echec(existe
                    ? MotifEchecOrganisateurEnum.DemandeNonEnAttente
                    : MotifEchecOrganisateurEnum.DemandeIntrouvable);
            }

            var organisateur = await _context.Organisateurs
                .AsNoTracking()
                .FirstAsync(o => o.Id == organisateurId);

            return ResultatOrganisateur.Reussi(organisateur);
        }


    }
}
