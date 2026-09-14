using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Domain.Entities
{
    public enum StatutPaiement
    {
        EnAttente,
        Paye,
        Annule,
    }
    public class Reservation
    {
        public Guid Id { get; set;  }
        public Guid UtilisateurId { get; set; }
        public DateTime DateReservation { get; set; } = DateTime.UtcNow;
        public StatutPaiement StatutPaiement { get; set; } = StatutPaiement.EnAttente;
        public decimal MontantTotal { get; set; } = 0m;

        public Utilisateur Utilisateur { get; set; } = null!;
        public ICollection<Billet> Billets { get; set; } = new List<Billet>();
    }
}
