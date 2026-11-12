using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data.Persistence;

public sealed class DatabaseMigrator
{
    private readonly SqliteConnectionFactory _factory;

    public DatabaseMigrator(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public void Migrate()
    {
        using var connection = _factory.Create();

        using (var createVersion = connection.CreateCommand())
        {
            createVersion.CommandText = @"
                CREATE TABLE IF NOT EXISTS VersionSchema (
                    Version INTEGER PRIMARY KEY,
                    AppliqueeLeUtc TEXT NOT NULL
                );";
            createVersion.ExecuteNonQuery();
        }

        using var readCurrent = connection.CreateCommand();
        readCurrent.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM VersionSchema;";
        var current = Convert.ToInt32(readCurrent.ExecuteScalar());

        if (current >= 1)
            return;

        using var tx = connection.BeginTransaction();

        using (var script = connection.CreateCommand())
        {
            script.Transaction = tx;
            script.CommandText = @"
                CREATE TABLE Utilisateur (
                    Id INTEGER PRIMARY KEY,
                    Nom TEXT NOT NULL,
                    Identifiant TEXT NOT NULL UNIQUE,
                    MotDePasseHash TEXT NOT NULL,
                    Sel TEXT NOT NULL,
                    ParametresHachage TEXT NOT NULL,
                    Role TEXT NOT NULL CHECK (Role IN ('Administrateur', 'Vendeur')),
                    Actif INTEGER NOT NULL CHECK (Actif IN (0, 1))
                );

                CREATE TABLE Categorie (
                    Id INTEGER PRIMARY KEY,
                    Nom TEXT NOT NULL UNIQUE
                );

                CREATE TABLE Produit (
                    Id INTEGER PRIMARY KEY,
                    Reference TEXT NOT NULL UNIQUE,
                    Nom TEXT NOT NULL,
                    CategorieId INTEGER NULL,
                    PrixVente INTEGER NOT NULL CHECK (PrixVente > 0),
                    SeuilAlerte INTEGER NOT NULL CHECK (SeuilAlerte >= 0),
                    Actif INTEGER NOT NULL CHECK (Actif IN (0, 1)),
                    DateCreation TEXT NOT NULL,
                    FOREIGN KEY (CategorieId) REFERENCES Categorie(Id) ON DELETE RESTRICT
                );

                CREATE TABLE Vente (
                    Id INTEGER PRIMARY KEY,
                    Numero TEXT NOT NULL UNIQUE,
                    JetonValidation TEXT NOT NULL UNIQUE,
                    DateHeureUtc TEXT NOT NULL,
                    DateMetier TEXT NOT NULL,
                    VendeurId INTEGER NOT NULL,
                    Total INTEGER NOT NULL CHECK (Total > 0),
                    ModePaiement TEXT NOT NULL,
                    MontantRecu INTEGER NOT NULL CHECK (MontantRecu >= 0),
                    MonnaieRendue INTEGER NOT NULL CHECK (MonnaieRendue >= 0),
                    Statut TEXT NOT NULL CHECK (Statut IN ('Validee', 'Annulee')),
                    FOREIGN KEY (VendeurId) REFERENCES Utilisateur(Id) ON DELETE RESTRICT
                );

                CREATE TABLE LigneVente (
                    Id INTEGER PRIMARY KEY,
                    VenteId INTEGER NOT NULL,
                    ProduitId INTEGER NOT NULL,
                    ReferenceSnapshot TEXT NOT NULL,
                    NomSnapshot TEXT NOT NULL,
                    Quantite INTEGER NOT NULL CHECK (Quantite > 0),
                    PrixUnitaire INTEGER NOT NULL CHECK (PrixUnitaire > 0),
                    SousTotal INTEGER NOT NULL CHECK (SousTotal > 0),
                    UNIQUE (VenteId, ProduitId),
                    FOREIGN KEY (VenteId) REFERENCES Vente(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (ProduitId) REFERENCES Produit(Id) ON DELETE RESTRICT
                );

                CREATE TABLE AnnulationVente (
                    Id INTEGER PRIMARY KEY,
                    VenteId INTEGER NOT NULL UNIQUE,
                    AuteurId INTEGER NOT NULL,
                    DateHeureUtc TEXT NOT NULL,
                    Motif TEXT NOT NULL,
                    FOREIGN KEY (VenteId) REFERENCES Vente(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (AuteurId) REFERENCES Utilisateur(Id) ON DELETE RESTRICT
                );

                CREATE TABLE MouvementStock (
                    Id INTEGER PRIMARY KEY,
                    ProduitId INTEGER NOT NULL,
                    AuteurId INTEGER NOT NULL,
                    DateHeureUtc TEXT NOT NULL,
                    Type TEXT NOT NULL CHECK (Type IN ('StockInitial', 'Reapprovisionnement', 'VenteValidee', 'AnnulationVente', 'CorrectionInventaire')),
                    QuantiteSignee INTEGER NOT NULL,
                    Motif TEXT NOT NULL,
                    LigneVenteId INTEGER NULL UNIQUE,
                    AnnulationId INTEGER NULL,
                    FOREIGN KEY (ProduitId) REFERENCES Produit(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (AuteurId) REFERENCES Utilisateur(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (LigneVenteId) REFERENCES LigneVente(Id) ON DELETE RESTRICT,
                    FOREIGN KEY (AnnulationId) REFERENCES AnnulationVente(Id) ON DELETE RESTRICT
                );

                CREATE TABLE JournalAudit (
                    Id INTEGER PRIMARY KEY,
                    AuteurId INTEGER NULL,
                    DateHeureUtc TEXT NOT NULL,
                    Action TEXT NOT NULL,
                    Entite TEXT NOT NULL,
                    EntiteId TEXT NOT NULL,
                    Details TEXT NOT NULL,
                    FOREIGN KEY (AuteurId) REFERENCES Utilisateur(Id) ON DELETE RESTRICT
                );

                CREATE TABLE Parametre (
                    Cle TEXT PRIMARY KEY NOT NULL,
                    Valeur TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_Produit_CategorieId ON Produit(CategorieId);
                CREATE INDEX IF NOT EXISTS IX_Vente_Vendeur_Date ON Vente(VendeurId, DateHeureUtc);
                CREATE INDEX IF NOT EXISTS IX_Vente_DateMetier ON Vente(DateMetier);
                CREATE INDEX IF NOT EXISTS IX_LigneVente_ProduitId ON LigneVente(ProduitId);
                CREATE INDEX IF NOT EXISTS IX_MouvementStock_Produit_Date ON MouvementStock(ProduitId, DateHeureUtc);
                CREATE INDEX IF NOT EXISTS IX_JournalAudit_Auteur_Date ON JournalAudit(AuteurId, DateHeureUtc);
            ;";
            script.ExecuteNonQuery();
        }

        using (var insertVersion = connection.CreateCommand())
        {
            insertVersion.Transaction = tx;
            insertVersion.CommandText = "INSERT INTO VersionSchema (Version, AppliqueeLeUtc) VALUES (1, @appliedAt);";
            insertVersion.Parameters.AddWithValue("@appliedAt", DateTime.UtcNow.ToString("O"));
            insertVersion.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
