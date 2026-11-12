namespace GestionMagasin.Core.Repositories;

public enum TypeMouvementStock
{
    StockInitial,
    Reapprovisionnement,
    VenteValidee,
    AnnulationVente,
    CorrectionInventaire
}

public sealed record MouvementStockVue(
    long Id,
    int ProduitId,
    string Reference,
    string Nom,
    DateTime DateHeureUtc,
    TypeMouvementStock Type,
    int QuantiteSignee,
    string Motif);

public sealed record CorrectionStockResultat(
    long StockAvant,
    long StockCompte,
    long Ecart);

public interface IStockRepository
{
    Task<IReadOnlyList<MouvementStockVue>> ListerMouvementsAsync(CancellationToken cancellationToken);
    Task<long> ObtenirStockAsync(int produitId, CancellationToken cancellationToken);
    Task<bool> AjouterMouvementAsync(
        int produitId,
        int auteurId,
        TypeMouvementStock type,
        int quantiteSignee,
        string motif,
        CancellationToken cancellationToken);
    Task<CorrectionStockResultat?> CorrigerInventaireAsync(
        int produitId,
        int auteurId,
        int quantiteComptee,
        string motif,
        CancellationToken cancellationToken);
}
