using Billetterie.Domain.Entities;

namespace Billetterie.Application.DTOs
{
    public class ReservationDto
    {
        public required Guid Id { get; set; }
        public required decimal MontantTotal { get; set; }
        public required DateTime DateExpiration { get; set; }
        public required StatutPaiement StatutPaiement { get; set; }
        public required IList<BilletDto> Billets { get; set; }
    }
}