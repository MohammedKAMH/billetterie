namespace Billetterie.Domain.Entities
{
    public enum BilletStatut
    {
        Valide,
        Utilise,
        Annule,
    }
    public class Billet
    {
        public Guid Id { get; set; }
        public Guid ReservationId { get; set; }
        public Guid TypeBilletId { get; set; }
        public string CodeUnique { get; set; } = string.Empty;
        public BilletStatut Statut { get; set; } = BilletStatut.Valide;
        public DateTime? DateUtilisation { get; set; }

        public Reservation Reservation { get; set; } = null!;
        public TypeBillet TypeBillet { get; set; } = null!;

    }
}
