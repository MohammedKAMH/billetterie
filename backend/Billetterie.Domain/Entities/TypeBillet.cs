using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Domain.Entities
{
    public class TypeBillet
    {
        public Guid Id { get; set;  }
        public Guid EvenementId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal Prix { get; set; } = 0m;
        public int QuantiteTotale { get; set;  }
        public int QuantiteVendue { get; set; }
        public int QuantiteDisponible => QuantiteTotale - QuantiteVendue;


        public Evenement Evenement { get; set; } = null!;
        public ICollection<Billet> Billets { get; set; } = new List<Billet>();
    }
}
