using Billetterie.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Application.DTOs
{
    public class BilletDto
    {
        public required Guid Id { get; set; }
        public required string NomTypeBillet { get; set; }
        public required BilletStatut Statut { get; set; }
        public string? CodeUnique { get; set; }
    }
}
