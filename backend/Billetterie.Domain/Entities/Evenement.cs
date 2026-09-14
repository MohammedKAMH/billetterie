using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Domain.Entities
{
    public enum EvenementStatut
    {
        Brouillon,
        Publie,
        Annule,
        Termine,
    }
    public class Evenement
    {
        public Guid Id { get; set; }
        public Guid OrganisateurId { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Lieu { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public EvenementStatut Statut { get; set; } = EvenementStatut.Brouillon;


        public Organisateur Organisateur { get; set; } = null!;
        public ICollection<TypeBillet> TypeBillet { get; set; } = new  List<TypeBillet>();
    }
}
