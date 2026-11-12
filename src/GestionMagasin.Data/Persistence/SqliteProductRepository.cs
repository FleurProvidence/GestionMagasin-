using GestionMagasin.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data.Persistence;

public sealed class SqliteProductRepository : IProduitRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteProductRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<ProduitCatalogue>> RechercherAsync(
        string? recherche,
        string? categorie,
        bool? actif,
        CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        var sql = @"
            SELECT p.Id, p.Reference, p.Nom, c.Nom, p.PrixVente, p.SeuilAlerte, p.Actif,
                   COALESCE(SUM(m.QuantiteSignee), 0)
            FROM Produit p
            LEFT JOIN Categorie c ON c.Id = p.CategorieId
            LEFT JOIN MouvementStock m ON m.ProduitId = p.Id
            WHERE (@recherche IS NULL OR p.Reference LIKE @recherche OR p.Nom LIKE @recherche)
              AND (@categorie IS NULL OR c.Nom = @categorie)
              AND (@actif IS NULL OR p.Actif = @actif)
            GROUP BY p.Id, p.Reference, p.Nom, c.Nom, p.PrixVente, p.SeuilAlerte, p.Actif
            ORDER BY p.Reference;";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@recherche", string.IsNullOrWhiteSpace(recherche) ? DBNull.Value : "%" + recherche.Trim() + "%");
        cmd.Parameters.AddWithValue("@categorie", string.IsNullOrWhiteSpace(categorie) ? DBNull.Value : categorie.Trim());
        cmd.Parameters.AddWithValue("@actif", actif is null ? DBNull.Value : actif.Value ? 1 : 0);

        var results = new List<ProduitCatalogue>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ProduitCatalogue(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetInt64(4),
                reader.GetInt32(5),
                reader.GetInt32(6) == 1,
                Convert.ToInt32(reader.GetInt64(7))));
        }

        return results;
    }

    public async Task<ProduitCatalogue?> TrouverParIdAsync(int produitId, CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT p.Id, p.Reference, p.Nom, c.Nom, p.PrixVente, p.SeuilAlerte, p.Actif,
                   COALESCE(SUM(m.QuantiteSignee), 0)
            FROM Produit p
            LEFT JOIN Categorie c ON c.Id = p.CategorieId
            LEFT JOIN MouvementStock m ON m.ProduitId = p.Id
            WHERE p.Id = @id
            GROUP BY p.Id, p.Reference, p.Nom, c.Nom, p.PrixVente, p.SeuilAlerte, p.Actif;";
        cmd.Parameters.AddWithValue("@id", produitId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new ProduitCatalogue(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetInt64(4),
            reader.GetInt32(5),
            reader.GetInt32(6) == 1,
            Convert.ToInt32(reader.GetInt64(7)));
    }

    public async Task<ResultatProduit> CreerAsync(
        CreerProduitCommande commande,
        int auteurId,
        CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        using (var check = connection.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT COUNT(*) FROM Produit WHERE Reference = @reference;";
            check.Parameters.AddWithValue("@reference", commande.Reference.Trim());
            var count = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken));
            if (count > 0)
            {
                tx.Rollback();
                return ResultatProduit.ReferenceDejaUtilisee;
            }
        }

        int? categorieId = null;
        if (!string.IsNullOrWhiteSpace(commande.Categorie))
        {
            using (var findCat = connection.CreateCommand())
            {
                findCat.Transaction = tx;
                findCat.CommandText = "SELECT Id FROM Categorie WHERE Nom = @nom;";
                findCat.Parameters.AddWithValue("@nom", commande.Categorie.Trim());
                var value = await findCat.ExecuteScalarAsync(cancellationToken);
                if (value is null)
                {
                    using var insertCat = connection.CreateCommand();
                    insertCat.Transaction = tx;
                    insertCat.CommandText = "INSERT INTO Categorie (Nom) VALUES (@nom);";
                    insertCat.Parameters.AddWithValue("@nom", commande.Categorie.Trim());
                    insertCat.ExecuteNonQuery();
                    categorieId = Convert.ToInt32(connection.LastInsertRowId);
                }
                else
                {
                    categorieId = Convert.ToInt32(value);
                }
            }
        }

        using (var insertProduit = connection.CreateCommand())
        {
            insertProduit.Transaction = tx;
            insertProduit.CommandText = @"
                INSERT INTO Produit (Reference, Nom, CategorieId, PrixVente, SeuilAlerte, Actif, DateCreation)
                VALUES (@reference, @nom, @categorieId, @prix, @seuil, 1, @date);";
            insertProduit.Parameters.AddWithValue("@reference", commande.Reference.Trim());
            insertProduit.Parameters.AddWithValue("@nom", commande.Nom.Trim());
            insertProduit.Parameters.AddWithValue("@categorieId", categorieId ?? DBNull.Value);
            insertProduit.Parameters.AddWithValue("@prix", commande.PrixVente);
            insertProduit.Parameters.AddWithValue("@seuil", commande.SeuilAlerte);
            insertProduit.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            insertProduit.ExecuteNonQuery();
        }

        var produitId = Convert.ToInt32(connection.LastInsertRowId);

        if (commande.StockInitial is > 0)
        {
            using (var insertMv = connection.CreateCommand())
            {
                insertMv.Transaction = tx;
                insertMv.CommandText = @"
                    INSERT INTO MouvementStock (ProduitId, AuteurId, DateHeureUtc, Type, QuantiteSignee, Motif, LigneVenteId, AnnulationId)
                    VALUES (@produitId, @auteurId, @date, 'StockInitial', @qty, @motif, NULL, NULL);";
                insertMv.Parameters.AddWithValue("@produitId", produitId);
                insertMv.Parameters.AddWithValue("@auteurId", auteurId);
                insertMv.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
                insertMv.Parameters.AddWithValue("@qty", commande.StockInitial.Value);
                insertMv.Parameters.AddWithValue("@motif", "Stock initial");
                insertMv.ExecuteNonQuery();
            }
        }

        using (var audit = connection.CreateCommand())
        {
            audit.Transaction = tx;
            audit.CommandText = @"
                INSERT INTO JournalAudit (AuteurId, DateHeureUtc, Action, Entite, EntiteId, Details)
                VALUES (@auteurId, @date, 'CreationProduit', 'Produit', @entiteId, @details);";
            audit.Parameters.AddWithValue("@auteurId", auteurId);
            audit.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            audit.Parameters.AddWithValue("@entiteId", produitId.ToString());
            audit.Parameters.AddWithValue("@details", "Création du produit.");
            audit.ExecuteNonQuery();
        }

        tx.Commit();
        return ResultatProduit.Succes;
    }

    public async Task<ResultatProduit> ModifierAsync(ModifierProduitCommande commande, int auteurId, CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        using (var existing = connection.CreateCommand())
        {
            existing.Transaction = tx;
            existing.CommandText = "SELECT COUNT(*) FROM Produit WHERE Id = @id;";
            existing.Parameters.AddWithValue("@id", commande.ProduitId);
            var count = Convert.ToInt32(await existing.ExecuteScalarAsync(cancellationToken));
            if (count == 0)
            {
                tx.Rollback();
                return ResultatProduit.Introuvable;
            }
        }

        using (var check = connection.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT COUNT(*) FROM Produit WHERE Reference = @reference AND Id <> @id;";
            check.Parameters.AddWithValue("@reference", commande.Reference.Trim());
            check.Parameters.AddWithValue("@id", commande.ProduitId);
            var found = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken));
            if (found > 0)
            {
                tx.Rollback();
                return ResultatProduit.ReferenceDejaUtilisee;
            }
        }

        int? categorieId = null;
        if (!string.IsNullOrWhiteSpace(commande.Categorie))
        {
            using (var findCat = connection.CreateCommand())
            {
                findCat.Transaction = tx;
                findCat.CommandText = "SELECT Id FROM Categorie WHERE Nom = @nom;";
                findCat.Parameters.AddWithValue("@nom", commande.Categorie.Trim());
                var value = await findCat.ExecuteScalarAsync(cancellationToken);
                if (value is null)
                {
                    using var insertCat = connection.CreateCommand();
                    insertCat.Transaction = tx;
                    insertCat.CommandText = "INSERT INTO Categorie (Nom) VALUES (@nom);";
                    insertCat.Parameters.AddWithValue("@nom", commande.Categorie.Trim());
                    insertCat.ExecuteNonQuery();
                    categorieId = Convert.ToInt32(connection.LastInsertRowId);
                }
                else
                {
                    categorieId = Convert.ToInt32(value);
                }
            }
        }

        using (var update = connection.CreateCommand())
        {
            update.Transaction = tx;
            update.CommandText = @"
                UPDATE Produit
                SET Reference = @reference,
                    Nom = @nom,
                    CategorieId = @categorieId,
                    PrixVente = @prix,
                    SeuilAlerte = @seuil
                WHERE Id = @id;";
            update.Parameters.AddWithValue("@reference", commande.Reference.Trim());
            update.Parameters.AddWithValue("@nom", commande.Nom.Trim());
            update.Parameters.AddWithValue("@categorieId", categorieId ?? DBNull.Value);
            update.Parameters.AddWithValue("@prix", commande.PrixVente);
            update.Parameters.AddWithValue("@seuil", commande.SeuilAlerte);
            update.Parameters.AddWithValue("@id", commande.ProduitId);
            update.ExecuteNonQuery();
        }

        using (var audit = connection.CreateCommand())
        {
            audit.Transaction = tx;
            audit.CommandText = @"
                INSERT INTO JournalAudit (AuteurId, DateHeureUtc, Action, Entite, EntiteId, Details)
                VALUES (@auteurId, @date, 'ModificationProduit', 'Produit', @entiteId, @details);";
            audit.Parameters.AddWithValue("@auteurId", auteurId);
            audit.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            audit.Parameters.AddWithValue("@entiteId", commande.ProduitId.ToString());
            audit.Parameters.AddWithValue("@details", "Modification du produit.");
            audit.ExecuteNonQuery();
        }

        tx.Commit();
        return ResultatProduit.Succes;
    }

    public async Task<ResultatProduit> ArchiverAsync(int produitId, int auteurId, CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        using (var check = connection.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT COUNT(*) FROM Produit WHERE Id = @id;";
            check.Parameters.AddWithValue("@id", produitId);
            var count = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken));
            if (count == 0)
            {
                tx.Rollback();
                return ResultatProduit.Introuvable;
            }
        }

        using (var archive = connection.CreateCommand())
        {
            archive.Transaction = tx;
            archive.CommandText = "UPDATE Produit SET Actif = 0 WHERE Id = @id;";
            archive.Parameters.AddWithValue("@id", produitId);
            archive.ExecuteNonQuery();
        }

        using (var audit = connection.CreateCommand())
        {
            audit.Transaction = tx;
            audit.CommandText = @"
                INSERT INTO JournalAudit (AuteurId, DateHeureUtc, Action, Entite, EntiteId, Details)
                VALUES (@auteurId, @date, 'ArchivageProduit', 'Produit', @entiteId, @details);";
            audit.Parameters.AddWithValue("@auteurId", auteurId);
            audit.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            audit.Parameters.AddWithValue("@entiteId", produitId.ToString());
            audit.Parameters.AddWithValue("@details", "Produit archivé.");
            audit.ExecuteNonQuery();
        }

        tx.Commit();
        return ResultatProduit.Succes;
    }

    public async Task<ResultatProduit> ReactiverAsync(int produitId, int auteurId, CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        using (var check = connection.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT COUNT(*) FROM Produit WHERE Id = @id;";
            check.Parameters.AddWithValue("@id", produitId);
            var count = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken));
            if (count == 0)
            {
                tx.Rollback();
                return ResultatProduit.Introuvable;
            }
        }

        using (var react = connection.CreateCommand())
        {
            react.Transaction = tx;
            react.CommandText = "UPDATE Produit SET Actif = 1 WHERE Id = @id;";
            react.Parameters.AddWithValue("@id", produitId);
            react.ExecuteNonQuery();
        }

        using (var audit = connection.CreateCommand())
        {
            audit.Transaction = tx;
            audit.CommandText = @"
                INSERT INTO JournalAudit (AuteurId, DateHeureUtc, Action, Entite, EntiteId, Details)
                VALUES (@auteurId, @date, 'ReactivationProduit', 'Produit', @entiteId, @details);";
            audit.Parameters.AddWithValue("@auteurId", auteurId);
            audit.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            audit.Parameters.AddWithValue("@entiteId", produitId.ToString());
            audit.Parameters.AddWithValue("@details", "Produit réactivé.");
            audit.ExecuteNonQuery();
        }

        tx.Commit();
        return ResultatProduit.Succes;
    }
}
