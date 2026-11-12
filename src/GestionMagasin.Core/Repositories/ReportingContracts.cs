using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestionMagasin.Core.Repositories;

public sealed record DashboardSnapshot(
    DateOnly DateMetier,
    long ChiffreAffairesJour,
    long NombreVentesJour,
    long UnitesVenduesJour,
    long ReferencesEnRupture,
    long ReferencesStockFaible);

public interface IReportingRepository
{
    Task<DashboardSnapshot> ChargerDashboardAsync(
        DateOnly dateMetier,
        int? vendeurId,
        CancellationToken cancellationToken);
}
