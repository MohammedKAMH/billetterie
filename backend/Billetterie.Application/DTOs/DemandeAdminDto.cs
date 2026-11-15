using Billetterie.Domain.Entities;

namespace Billetterie.Application.DTOs
{
    public class DemandeAdminDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public StatutValidation StatutValidation { get; set; }
        public DateTime? DateValidation { get; set; }
    }
}
