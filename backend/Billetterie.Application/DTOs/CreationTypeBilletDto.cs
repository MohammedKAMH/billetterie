using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Billetterie.Application.DTOs
{
    public class CreationTypeBilletDto
    {
        [Required, StringLength(100)]
        public required string Nom { get; set; }

        [Range(0,100000)]
        public decimal Prix { get; set; }

        [Range(0, 100000)]
        public int QuantiteTotale { get; set; }
    }
}
