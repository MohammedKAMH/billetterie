using Billetterie.Application.ResultsEnum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Billetterie.Application.Interfaces
{
    public interface IReservationService
    {
        Task<ResultatReservation> CreerReservationAsync(Guid utilisateurId, IReadOnlyList<LigneDemande> lignes);
    }
}
