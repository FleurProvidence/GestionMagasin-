using GestionMagasin.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data.Persistence;

public sealed class SqliteStockRepository : IStockRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteStockRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<MouvementStockVue>> ListerMouvementsAsync(CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT m.Id, m.ProduitId, p.Reference, p.Nom, m.DateHeureUtc, m.Type, m.QuantiteSignee, m.Motif
            FROM MouvementStock m
            JOIN Produit p ON p.Id = m.ProduitId
            ORDER BY m.DateHeureUtc DESC;";

        var results = new List<MouvementStockVue>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MouvementStockVue(
                reader.GetInt64(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
                ParseType(reader.GetString(5)),
                reader.GetInt32(6),
                reader.GetString(7)));
        }

        return results;
    }

    public async Task<long> ObtenirStockAsync(int produitId, CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COALESCE(SUM(QuantiteSignee), 0)
            FROM MouvementStock
            WHERE ProduitId = @produitId;";
        command.Parameters.AddWithValue("@produitId", produitId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    public async Task<bool> AjouterMouvementAsync(
        int produitId,
        int auteurId,
        TypeMouvementStock type,
        int quantiteSignee,
        string motif,
        CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        using (var check = connection.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT COUNT(*), Actif FROM Produit WHERE Id = @id;";
            check.Parameters.AddWithValue("@id", produitId);
            using var reader = await check.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                tx.Rollback();
                return false;
            }

            var count = reader.GetInt32(0);
            var actif = reader.GetInt32(1) == 1;
            if (count == 0 || !actif)
            {
                tx.Rollback();
                return false;
            }
        }

        var stockActuel = await ObtenirStockInterneAsync(connection, tx, produitId, cancellationToken);
        if (quantiteSignee < 0 && (stockActuel + quantiteSignee) < 0)
        {
            tx.Rollback();
            return false;
        }

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = tx;
            insert.CommandText = @"
                INSERT INTO MouvementStock (ProduitId, AuteurId, DateHeureUtc, Type, QuantiteSignee, Motif, LigneVenteId, AnnulationId)
                VALUES (@produitId, @auteurId, @date, @type, @qty, @motif, NULL, NULL);";
            insert.Parameters.AddWithValue("@produitId", produitId);
            insert.Parameters.AddWithValue("@auteurId", auteurId);
            insert.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            insert.Parameters.AddWithValue("@type", TypeToString(type));
            insert.Parameters.AddWithValue("@qty", quantiteSignee);
            insert.Parameters.AddWithValue("@motif", motif);
            insert.ExecuteNonQuery();
        }

        tx.Commit();
        return true;
    }

    public async Task<CorrectionStockResultat?> CorrigerInventaireAsync(
        int produitId,
        int auteurId,
        int quantiteComptee,
        string motif,
        CancellationToken cancellationToken)
    {
        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        var stockAvant = await ObtenirStockInterneAsync(connection, tx, produitId, cancellationToken);
        var ecart = quantiteComptee - stockAvant;

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = tx;
            insert.CommandText = @"
                INSERT INTO MouvementStock (ProduitId, AuteurId, DateHeureUtc, Type, QuantiteSignee, Motif, LigneVenteId, AnnulationId)
                VALUES (@produitId, @auteurId, @date, 'CorrectionInventaire', @qty, @motif, NULL, NULL);";
            insert.Parameters.AddWithValue("@produitId", produitId);
            insert.Parameters.AddWithValue("@auteurId", auteurId);
            insert.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("O"));
            insert.Parameters.AddWithValue("@qty", ecart);
            insert.Parameters.AddWithValue("@motif", motif);
            insert.ExecuteNonQuery();
        }

        tx.Commit();
        return new CorrectionStockResultat(stockAvant, quantiteComptee, ecart);
    }

    private static async Task<long> ObtenirStockInterneAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        int produitId,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.Transaction = tx;
        command.CommandText = "SELECT COALESCE(SUM(QuantiteSignee), 0) FROM MouvementStock WHERE ProduitId = @id;";
        command.Parameters.AddWithValue("@id", produitId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static TypeMouvementStock ParseType(string value) => value switch
    {
        "StockInitial" => TypeMouvementStock.StockInitial,
        "Reapprovisionnement" => TypeMouvementStock.Reapprovisionnement,
        "VenteValidee" => TypeMouvementStock.VenteValidee,
        "AnnulationVente" => TypeMouvementStock.AnnulationVente,
        "CorrectionInventaire" => TypeMouvementStock.CorrectionInventaire,
        _ => throw new InvalidOperationException("Type de mouvement inconnu.")
    };

    private static string TypeToString(TypeMouvementStock type) => type switch
    {
        TypeMouvementStock.StockInitial => "StockInitial",
        TypeMouvementStock.Reapprovisionnement => "Reapprovisionnement",
        TypeMouvementStock.VenteValidee => "VenteValidee",
        TypeMouvementStock.AnnulationVente => "AnnulationVente",
        TypeMouvementStock.CorrectionInventaire => "CorrectionInventaire",
        _ => throw new InvalidOperationException("Type de mouvement inconnu.")
    };
}
