using Billetterie.Domain.Entities;

namespace Billetterie.Application.ResultsEnum
{
    public class ResultatReservation
    {
        public bool Succes { get; }
        public Reservation? Reservation { get; }
        public MotifEchecEnum? Motif { get; }

        private ResultatReservation(bool succes, Reservation? reservation, MotifEchecEnum? motif)
        {
            Succes = succes;
            Reservation = reservation;
            Motif = motif;
        }

        public static ResultatReservation Reussi(Reservation reservation)
            => new(true, reservation, null);

        public static ResultatReservation Echec(MotifEchecEnum motif)
            => new(false, null, motif);
    }
}