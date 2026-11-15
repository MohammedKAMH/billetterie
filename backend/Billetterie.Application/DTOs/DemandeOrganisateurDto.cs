using Billetterie.Domain.Entities;

namespace Billetterie.Application.DTOs
{
    public class DemandeOrganisateurDto
    {
        public StatutValidation StatutValidation { get; set; }
        public DateTime? DateValidation { get; set; }
    }
}
