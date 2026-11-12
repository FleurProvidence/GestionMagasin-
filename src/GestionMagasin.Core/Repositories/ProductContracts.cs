namespace GestionMagasin.Core.Repositories;

public sealed record ProduitCatalogue(
    int Id,
    string Reference,
    string Nom,
    string? Categorie,
    long PrixVente,
    int SeuilAlerte,
    bool Actif,
    int StockDisponible);

public sealed record CreerProduitCommande(
    string Reference,
    string Nom,
    string? Categorie,
    long PrixVente,
    int SeuilAlerte,
    int? StockInitial);

public sealed record ModifierProduitCommande(
    int ProduitId,
    string Reference,
    string Nom,
    string? Categorie,
    long PrixVente,
    int SeuilAlerte);

public enum ResultatProduit
{
    Succes,
    Introuvable,
    ReferenceDejaUtilisee,
    ProduitArchive,
    StockInvalide
}

public interface IProduitRepository
{
    Task<IReadOnlyList<ProduitCatalogue>> RechercherAsync(
        string? recherche,
        string? categorie,
        bool? actif,
        CancellationToken cancellationToken);

    Task<ProduitCatalogue?> TrouverParIdAsync(
        int produitId,
        CancellationToken cancellationToken);

    Task<ResultatProduit> CreerAsync(
        CreerProduitCommande commande,
        int auteurId,
        CancellationToken cancellationToken);

    Task<ResultatProduit> ModifierAsync(
        ModifierProduitCommande commande,
        int auteurId,
        CancellationToken cancellationToken);

    Task<ResultatProduit> ArchiverAsync(
        int produitId,
        int auteurId,
        CancellationToken cancellationToken);

    Task<ResultatProduit> ReactiverAsync(
        int produitId,
        int auteurId,
        CancellationToken cancellationToken);
}
