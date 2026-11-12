using GestionMagasin.Core.Models;
using GestionMagasin.Data.Persistence;

namespace GestionMagasin.Services.Security;

public sealed class AuthService
{
    private readonly SqliteConnectionFactory _factory;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _session;

    public AuthService(SqliteConnectionFactory factory, PasswordHasher passwordHasher, SessionService session)
    {
        _factory = factory;
        _passwordHasher = passwordHasher;
        _session = session;
    }

    public bool IsFirstLaunch()
    {
        using var connection = _factory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Utilisateur;";
        var count = Convert.ToInt32(command.ExecuteScalar());
        return count == 0;
    }

    public Utilisateur? Login(string identifiant, string password)
    {
        using var connection = _factory.Create();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Nom, Identifiant, MotDePasseHash, Sel, ParametresHachage, Role, Actif
            FROM Utilisateur
            WHERE Identifiant = @identifiant;";

        command.Parameters.AddWithValue("@identifiant", identifiant);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var user = new Utilisateur
        {
            Id = reader.GetInt32(0),
            Nom = reader.GetString(1),
            Identifiant = reader.GetString(2),
            MotDePasseHash = reader.GetString(3),
            Sel = reader.GetString(4),
            ParametresHachage = reader.GetString(5),
            Role = Enum.Parse<RoleUtilisateur>(reader.GetString(6)),
            Actif = reader.GetInt32(7) == 1
        };

        if (!user.Actif)
            return null;

        var valid = _passwordHasher.VerifyPassword(
            password,
            user.MotDePasseHash,
            user.Sel,
            user.ParametresHachage);

        if (!valid)
            return null;

        _session.Login(user);
        return user;
    }

    public void CreateFirstAdmin(string nom, string identifiant, string password)
    {
        if (string.IsNullOrWhiteSpace(nom)) throw new ArgumentException("Le nom est requis.");
        if (string.IsNullOrWhiteSpace(identifiant)) throw new ArgumentException("L'identifiant est requis.");
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Le mot de passe est requis.");

        var hash = _passwordHasher.HashPassword(password);

        using var connection = _factory.Create();
        using var tx = connection.BeginTransaction();

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = tx;
            insert.CommandText = @"
                INSERT INTO Utilisateur
                    (Nom, Identifiant, MotDePasseHash, Sel, ParametresHachage, Role, Actif)
                VALUES
                    (@nom, @identifiant, @hash, @sel, @params, @role, 1);";
            insert.Parameters.AddWithValue("@nom", nom);
            insert.Parameters.AddWithValue("@identifiant", identifiant);
            insert.Parameters.AddWithValue("@hash", hash.Hash);
            insert.Parameters.AddWithValue("@sel", hash.Sel);
            insert.Parameters.AddWithValue("@params", hash.Parametres);
            insert.Parameters.AddWithValue("@role", RoleUtilisateur.Administrateur.ToString());
            insert.ExecuteNonQuery();
        }

        using (var audit = connection.CreateCommand())
        {
            audit.Transaction = tx;
            audit.CommandText = @"
                INSERT INTO JournalAudit
                    (AuteurId, DateHeureUtc, Action, Entite, EntiteId, Details)
                VALUES
                    (NULL, @utc, 'CreationPremierAdministrateur', 'Utilisateur', @identifiant, 'Premier administrateur créé.');";
            audit.Parameters.AddWithValue("@utc", DateTime.UtcNow.ToString("O"));
            audit.Parameters.AddWithValue("@identifiant", identifiant);
            audit.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
