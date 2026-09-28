using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Billetterie.Application.DTOs
{
    public class CreationEvenementDto
    {
        public required string Titre { get; set; }
        public required string Description { get; set; }
        public required string Lieu { get; set; }
        public required DateTime DateDebut { get; set; }
        public required DateTime DateFin { get; set; }

    }
}
