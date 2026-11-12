using GestionMagasin.Core.Repositories;
using GestionMagasin.Services.Security;

namespace GestionMagasin.Services.Reporting;

public sealed class DashboardService
{
    private readonly IReportingRepository _repo;
    private readonly SessionService _session;

    public DashboardService(IReportingRepository repo, SessionService session)
    {
        _repo = repo;
        _session = session;
    }

    public async Task<DashboardSnapshot> ChargerAsync(DateOnly dateMetier, CancellationToken cancellationToken = default)
    {
        _session.RequireUser();
        var user = _session.RequireUser();
        int? vendeurId = user.Role == GestionMagasin.Core.Models.RoleUtilisateur.Administrateur ? null : user.Id;
        return await _repo.ChargerDashboardAsync(dateMetier, vendeurId, cancellationToken);
    }
}
