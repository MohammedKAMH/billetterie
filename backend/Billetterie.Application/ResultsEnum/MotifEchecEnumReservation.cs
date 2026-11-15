using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Application.ResultsEnum
{
    public enum MotifEchecEnumReservation
    {
        TypeBilletIntrouvable,
        EvenementNonPublie,
        TypesBilletDEvenementsDifferents,
        EvenementDejaCommence,
        StockInsuffisant,
        LigneEnDouble,
        ReservationIntrouvable,
        ReservationNonPayable,
        ReservationExpiree,
        ReservationNonAnnulable,
    }
}
