namespace GestionMagasin.Core.Repositories;

public enum ModePaiement
{
    Especes,
    MobileMoney,
    Carte
}

public sealed record LignePanier(
    int ProduitId,
    string Reference,
    string Nom,
    int Quantite,
    long PrixUnitaire,
    long SousTotal);

public sealed record ValiderVenteCommande(
    Guid JetonValidation,
    int VendeurId,
    DateTime DateHeureUtc,
    DateOnly DateMetier,
    ModePaiement ModePaiement,
    long MontantRecu,
    IReadOnlyList<LignePanier> Lignes);

public enum ResultatValidationVente
{
    Validee,
    PrixModifie,
    StockInsuffisant,
    ProduitIndisponible,
    DoubleValidation,
    PanierVide
}

public sealed record ResultatValidationVenteResponse(
    ResultatValidationVente Resultat,
    long? VenteId = null,
    string? Numero = null,
    int? ProduitId = null,
    string? ReferenceProduit = null,
    long? StockDisponible = null);

public interface ISaleRepository
{
    Task<ResultatValidationVenteResponse> ValiderVenteAsync(
        ValiderVenteCommande commande,
        CancellationToken cancellationToken);
}
