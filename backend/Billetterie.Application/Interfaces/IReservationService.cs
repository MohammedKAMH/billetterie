using Billetterie.Application.ResultsEnum;
using Billetterie.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Application.Interfaces
{
    public interface IReservationService
    {
        Task<ResultatReservation> CreerReservationAsync(Guid utilisateurId, IReadOnlyList<LigneDemande> lignes);

        Task<ResultatReservation> PayerReservationAsync(Guid utilisateurId, Guid reservationId);

        Task<ResultatReservation> AnnulerReservationAsync(Guid utilisateurId, Guid reservationId);

        Task<IList<Reservation>> GetReservationsAsync(Guid utilisateurId);
        Task<Reservation?> GetReservationByIdAsync(Guid utilisateurId, Guid reservationId);
        Task<int> ExpirerReservationsAsync(CancellationToken ct = default);
    }
}
