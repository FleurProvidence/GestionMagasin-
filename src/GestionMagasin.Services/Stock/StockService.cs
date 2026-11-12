using GestionMagasin.Core.Repositories;
using GestionMagasin.Services.Security;

namespace GestionMagasin.Services.Stock;

public sealed class StockService
{
    private readonly IStockRepository _repo;
    private readonly SessionService _session;

    public StockService(IStockRepository repo, SessionService session)
    {
        _repo = repo;
        _session = session;
    }

    public async Task<long> ObtenirStockAsync(int produitId, CancellationToken cancellationToken = default)
    {
        _session.RequireUser();
        if (produitId <= 0)
            throw new ArgumentOutOfRangeException(nameof(produitId));

        return await _repo.ObtenirStockAsync(produitId, cancellationToken);
    }

    public async Task<IReadOnlyList<MouvementStockVue>> ListerMouvementsAsync(CancellationToken cancellationToken = default)
    {
        _session.RequireUser();
        return await _repo.ListerMouvementsAsync(cancellationToken);
    }

    public async Task<bool> ReapprovisionnerAsync(int produitId, int quantite, string motif, CancellationToken cancellationToken = default)
    {
        var user = _session.RequireUser();
        if (user.Role != GestionMagasin.Core.Models.RoleUtilisateur.Administrateur)
            throw new UnauthorizedAccessException("Réservé aux administrateurs.");

        if (produitId <= 0)
            throw new ArgumentOutOfRangeException(nameof(produitId));

        if (quantite <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantite));

        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Le motif est obligatoire.");

        return await _repo.AjouterMouvementAsync(
            produitId,
            user.Id,
            TypeMouvementStock.Reapprovisionnement,
            quantite,
            motif,
            cancellationToken);
    }

    public async Task<CorrectionStockResultat?> CorrigerInventaireAsync(
        int produitId,
        int quantiteComptee,
        string motif,
        CancellationToken cancellationToken = default)
    {
        var user = _session.RequireUser();
        if (user.Role != GestionMagasin.Core.Models.RoleUtilisateur.Administrateur)
            throw new UnauthorizedAccessException("Réservé aux administrateurs.");

        if (produitId <= 0)
            throw new ArgumentOutOfRangeException(nameof(produitId));

        if (quantiteComptee < 0)
            throw new ArgumentOutOfRangeException(nameof(quantiteComptee));

        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Le motif est obligatoire.");

        return await _repo.CorrigerInventaireAsync(
            produitId,
            user.Id,
            quantiteComptee,
            motif,
            cancellationToken);
    }
}
