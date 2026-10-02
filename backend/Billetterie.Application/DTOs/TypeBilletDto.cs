using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Billetterie.Application.DTOs
{
    public class TypeBilletDto
    {
        public required Guid Id { get; set; }

        public required string Nom { get; set; }

        public  decimal Prix { get; set; }
        public  int QuantiteDisponible { get; set; }


    }
}
