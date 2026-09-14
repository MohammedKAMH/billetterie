using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Domain.Entities
{
    public enum StatutValidation
    {
        EnAttente,
        Valide,
        Refuse,
    }

    public class Organisateur
    {
        public Guid Id { get; set; }
        public Guid UtilisateurId { get; set;}
        public StatutValidation StatutValidation { get; set; } = StatutValidation.EnAttente;  
        public Guid? ValideParUtilisateurId { get; set; }
        public DateTime? DateValidation { get; set; }
        public Utilisateur Utilisateur { get; set; } = null!;
        public ICollection<Evenement> Evenements { get; set; } = new List<Evenement>();

    
    }
}
