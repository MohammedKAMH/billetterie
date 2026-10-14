using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Billetterie.Application.DTOs
{
    public class LigneReservationDto
    {
        public required Guid TypeBilletId { get; set; }

        [Range(1, 10)]
        public required int Quantite { get; set; }
    }
}
