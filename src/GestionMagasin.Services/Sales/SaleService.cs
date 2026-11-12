using GestionMagasin.Core.Repositories;
using GestionMagasin.Services.Security;

namespace GestionMagasin.Services.Sales;

public sealed class SaleService
{
    private readonly ISaleRepository _repo;
    private readonly SessionService _session;

    public SaleService(ISaleRepository repo, SessionService session)
    {
        _repo = repo;
        _session = session;
    }

    public async Task<ResultatValidationVenteResponse> ValiderVenteAsync(
        Guid jetonValidation,
        DateOnly dateMetier,
        ModePaiement modePaiement,
        long montantRecu,
        IReadOnlyList<LignePanier> lignes,
        CancellationToken cancellationToken = default)
    {
        var user = _session.RequireUser();
        if (jetonValidation == Guid.Empty)
            throw new ArgumentException("Le jeton de validation est obligatoire.");

        if (lignes.Count == 0)
            return new ResultatValidationVenteResponse(ResultatValidationVente.PanierVide);

        foreach (var ligne in lignes)
        {
            if (ligne.ProduitId <= 0 || ligne.Quantite <= 0 || ligne.PrixUnitaire <= 0)
                throw new ArgumentException("Une ligne du panier est invalide.");
        }

        long total = 0;
        foreach (var ligne in lignes)
            total += ligne.PrixUnitaire * ligne.Quantite;

        if (modePaiement == ModePaiement.Especes && montantRecu < total)
            throw new ArgumentException("La somme remise doit couvrir le total.");

        var commande = new ValiderVenteCommande(
            jetonValidation,
            user.Id,
            DateTime.UtcNow,
            dateMetier,
            modePaiement,
            montantRecu,
            lignes);

        return await _repo.ValiderVenteAsync(commande, cancellationToken);
    }
}
