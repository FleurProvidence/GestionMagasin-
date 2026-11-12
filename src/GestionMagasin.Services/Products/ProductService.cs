using GestionMagasin.Core.Repositories;
using GestionMagasin.Services.Security;

namespace GestionMagasin.Services.Products;

public sealed class ProductService
{
    private readonly IProduitRepository _repo;
    private readonly SessionService _session;

    public ProductService(IProduitRepository repo, SessionService session)
    {
        _repo = repo;
        _session = session;
    }

    public Task<IReadOnlyList<ProduitCatalogue>> RechercherAsync(
        string? recherche,
        string? categorie,
        bool? actif,
        CancellationToken cancellationToken = default)
    {
        _session.RequireUser();
        return _repo.RechercherAsync(recherche, categorie, actif, cancellationToken);
    }

    public async Task<ProduitCatalogue?> TrouverAsync(int produitId, CancellationToken cancellationToken = default)
    {
        _session.RequireUser();
        if (produitId <= 0)
            throw new ArgumentOutOfRangeException(nameof(produitId));

        return await _repo.TrouverParIdAsync(produitId, cancellationToken);
    }

    public async Task<ResultatProduit> CreerAsync(CreerProduitCommande commande, CancellationToken cancellationToken = default)
    {
        var user = _session.RequireUser();
        if (user.Role != GestionMagasin.Core.Models.RoleUtilisateur.Administrateur)
            throw new UnauthorizedAccessException("Réservé aux administrateurs.");

        if (string.IsNullOrWhiteSpace(commande.Reference))
            throw new ArgumentException("La référence est obligatoire.");

        if (string.IsNullOrWhiteSpace(commande.Nom))
            throw new ArgumentException("Le nom est obligatoire.");

        if (commande.PrixVente <= 0)
            throw new ArgumentException("Le prix de vente doit être > 0.");

        if (commande.SeuilAlerte < 0)
            throw new ArgumentException("Le seuil d’alerte doit être >= 0.");

        return await _repo.CreerAsync(commande, user.Id, cancellationToken);
    }

    public async Task<ResultatProduit> ModifierAsync(ModifierProduitCommande commande, CancellationToken cancellationToken = default)
    {
        var user = _session.RequireUser();
        if (user.Role != GestionMagasin.Core.Models.RoleUtilisateur.Administrateur)
            throw new UnauthorizedAccessException("Réservé aux administrateurs.");

        if (commande.ProduitId <= 0)
            throw new ArgumentOutOfRangeException(nameof(commande.ProduitId));

        if (string.IsNullOrWhiteSpace(commande.Reference))
            throw new ArgumentException("La référence est obligatoire.");

        if (string.IsNullOrWhiteSpace(commande.Nom))
            throw new ArgumentException("Le nom est obligatoire.");

        if (commande.PrixVente <= 0)
            throw new ArgumentException("Le prix de vente doit être > 0.");

        if (commande.SeuilAlerte < 0)
            throw new ArgumentException("Le seuil d’alerte doit être >= 0.");

        return await _repo.ModifierAsync(commande, user.Id, cancellationToken);
    }

    public async Task<ResultatProduit> ArchiverAsync(int produitId, CancellationToken cancellationToken = default)
    {
        var user = _session.RequireUser();
        if (user.Role != GestionMagasin.Core.Models.RoleUtilisateur.Administrateur)
            throw new UnauthorizedAccessException("Réservé aux administrateurs.");

        return await _repo.ArchiverAsync(produitId, user.Id, cancellationToken);
    }

    public async Task<ResultatProduit> ReactiverAsync(int produitId, CancellationToken cancellationToken = default)
    {
        var user = _session.RequireUser();
        if (user.Role != GestionMagasin.Core.Models.RoleUtilisateur.Administrateur)
            throw new UnauthorizedAccessException("Réservé aux administrateurs.");

        return await _repo.ReactiverAsync(produitId, user.Id, cancellationToken);
    }
}
