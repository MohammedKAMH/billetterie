using System.ComponentModel.DataAnnotations;

namespace Billetterie.Application.DTOs
{

    public class CreationReservationDto
    {
        [Required, MinLength(1), MaxLength(10)]
        public required List<LigneReservationDto> Lignes { get; set; }
    }
}