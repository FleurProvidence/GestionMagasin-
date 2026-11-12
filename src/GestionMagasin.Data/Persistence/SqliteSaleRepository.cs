using GestionMagasin.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data.Persistence;

public sealed class SqliteSaleRepository : ISaleRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteSaleRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<ResultatValidationVenteResponse> ValiderVenteAsync(
        ValiderVenteCommande commande,
        CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        using (var tokenCheck = connection.CreateCommand())
        {
            tokenCheck.Transaction = tx;
            tokenCheck.CommandText = "SELECT COUNT(*) FROM Vente WHERE JetonValidation = @token;";
            tokenCheck.Parameters.AddWithValue("@token", commande.JetonValidation.ToString());
            var exists = Convert.ToInt32(await tokenCheck.ExecuteScalarAsync(cancellationToken));
            if (exists > 0)
            {
                tx.Rollback();
                return new ResultatValidationVenteResponse(ResultatValidationVente.DoubleValidation);
            }
        }

        long total = 0;
        foreach (var ligne in commande.Lignes)
            total += ligne.PrixUnitaire * ligne.Quantite;

        foreach (var ligne in commande.Lignes)
        {
            using var produitCheck = connection.CreateCommand();
            produitCheck.Transaction = tx;
            produitCheck.CommandText = @"
                SELECT p.Id, p.Reference, p.Nom, p.PrixVente, p.Actif,
                       COALESCE(SUM(m.QuantiteSignee), 0)
                FROM Produit p
                LEFT JOIN MouvementStock m ON m.ProduitId = p.Id
                WHERE p.Id = @id
                GROUP BY p.Id, p.Reference, p.Nom, p.PrixVente, p.Actif;";
            produitCheck.Parameters.AddWithValue("@id", ligne.ProduitId);

            using var reader = await produitCheck.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                tx.Rollback();
                return new ResultatValidationVenteResponse(ResultatValidationVente.ProduitIndisponible, ProduitId: ligne.ProduitId, ReferenceProduit: ligne.Reference);
            }

            var prixActuel = reader.GetInt64(3);
            var actif = reader.GetInt32(4) == 1;
            var stock = Convert.ToInt64(reader.GetInt64(5));

            if (!actif)
            {
                tx.Rollback();
                return new ResultatValidationVenteResponse(ResultatValidationVente.ProduitIndisponible, ProduitId: ligne.ProduitId, ReferenceProduit: ligne.Reference);
            }

            if (prixActuel != ligne.PrixUnitaire)
            {
                tx.Rollback();
                return new ResultatValidationVenteResponse(ResultatValidationVente.PrixModifie);
            }

            if (stock < ligne.Quantite)
            {
                tx.Rollback();
                return new ResultatValidationVenteResponse(ResultatValidationVente.StockInsuffisant, ProduitId: ligne.ProduitId, ReferenceProduit: ligne.Reference, StockDisponible: stock);
            }
        }

        var numero = "V-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

        using (var insertVente = connection.CreateCommand())
        {
            insertVente.Transaction = tx;
            insertVente.CommandText = @"
                INSERT INTO Vente (Numero, JetonValidation, DateHeureUtc, DateMetier, VendeurId, Total, ModePaiement, MontantRecu, MonnaieRendue, Statut)
                VALUES (@numero, @jeton, @dateUtc, @dateMetier, @vendeurId, @total, @mode, @recu, @rendu, 'Validee');";
            insertVente.Parameters.AddWithValue("@numero", numero);
            insertVente.Parameters.AddWithValue("@jeton", commande.JetonValidation.ToString());
            insertVente.Parameters.AddWithValue("@dateUtc", commande.DateHeureUtc.ToString("O"));
            insertVente.Parameters.AddWithValue("@dateMetier", commande.DateMetier.ToString("yyyy-MM-dd"));
            insertVente.Parameters.AddWithValue("@vendeurId", commande.VendeurId);
            insertVente.Parameters.AddWithValue("@total", total);
            insertVente.Parameters.AddWithValue("@mode", commande.ModePaiement.ToString());
            insertVente.Parameters.AddWithValue("@recu", commande.MontantRecu);
            insertVente.Parameters.AddWithValue("@rendu", commande.MontantRecu - total);
            insertVente.ExecuteNonQuery();
        }

        var venteId = connection.LastInsertRowId;

        foreach (var ligne in commande.Lignes)
        {
            using (var insertLigne = connection.CreateCommand())
            {
                insertLigne.Transaction = tx;
                insertLigne.CommandText = @"
                    INSERT INTO LigneVente (VenteId, ProduitId, ReferenceSnapshot, NomSnapshot, Quantite, PrixUnitaire, SousTotal)
                    VALUES (@venteId, @produitId, @reference, @nom, @quantite, @prix, @sousTotal);";
                insertLigne.Parameters.AddWithValue("@venteId", venteId);
                insertLigne.Parameters.AddWithValue("@produitId", ligne.ProduitId);
                insertLigne.Parameters.AddWithValue("@reference", ligne.Reference);
                insertLigne.Parameters.AddWithValue("@nom", ligne.Nom);
                insertLigne.Parameters.AddWithValue("@quantite", ligne.Quantite);
                insertLigne.Parameters.AddWithValue("@prix", ligne.PrixUnitaire);
                insertLigne.Parameters.AddWithValue("@sousTotal", ligne.SousTotal);
                insertLigne.ExecuteNonQuery();
            }

            var ligneId = connection.LastInsertRowId;

            using (var insertMvt = connection.CreateCommand())
            {
                insertMvt.Transaction = tx;
                insertMvt.CommandText = @"
                    INSERT INTO MouvementStock (ProduitId, AuteurId, DateHeureUtc, Type, QuantiteSignee, Motif, LigneVenteId, AnnulationId)
                    VALUES (@produitId, @auteurId, @date, 'VenteValidee', @qty, @motif, @ligneId, NULL);";
                insertMvt.Parameters.AddWithValue("@produitId", ligne.ProduitId);
                insertMvt.Parameters.AddWithValue("@auteurId", commande.VendeurId);
                insertMvt.Parameters.AddWithValue("@date", commande.DateHeureUtc.ToString("O"));
                insertMvt.Parameters.AddWithValue("@qty", -ligne.Quantite);
                insertMvt.Parameters.AddWithValue("@motif", "Vente " + numero);
                insertMvt.Parameters.AddWithValue("@ligneId", ligneId);
                insertMvt.ExecuteNonQuery();
            }
        }

        using (var audit = connection.CreateCommand())
        {
            audit.Transaction = tx;
            audit.CommandText = @"
                INSERT INTO JournalAudit (AuteurId, DateHeureUtc, Action, Entite, EntiteId, Details)
                VALUES (@auteurId, @date, 'ValidationVente', 'Vente', @entiteId, @details);";
            audit.Parameters.AddWithValue("@auteurId", commande.VendeurId);
            audit.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            audit.Parameters.AddWithValue("@entiteId", venteId.ToString());
            audit.Parameters.AddWithValue("@details", "Validation vente " + numero);
            audit.ExecuteNonQuery();
        }

        tx.Commit();
        return new ResultatValidationVenteResponse(ResultatValidationVente.Validee, venteId, numero);
    }
}
