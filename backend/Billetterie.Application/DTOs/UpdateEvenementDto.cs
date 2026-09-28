using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Application.DTOs
{
    public class UpdateEvenementDto
    {
        public string Titre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Lieu { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }
}
