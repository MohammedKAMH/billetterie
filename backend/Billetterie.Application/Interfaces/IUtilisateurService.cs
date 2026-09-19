using Billetterie.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Application.Interfaces
{
    public interface IUtilisateurService
    {
        Task<Utilisateur?> GetUtilisateurAsync(string keycloakId);
        Task AddUtilisateurAsync(Utilisateur utilisateur);
        Task UpdateUtilisateurAsync(Utilisateur utilisateur);
    }
}
