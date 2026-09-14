using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Domain.Entities
{
    public class Utilisateur
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string KeycloakId { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;

        public Organisateur? Organisateur { get; set; }
        public ICollection<Reservation>? Reservations { get; set; }

    }

}
