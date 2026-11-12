namespace GestionMagasin.Core.Models;

public sealed class Utilisateur
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Identifiant { get; set; } = string.Empty;
    public string MotDePasseHash { get; set; } = string.Empty;
    public string Sel { get; set; } = string.Empty;
    public string ParametresHachage { get; set; } = string.Empty;
    public RoleUtilisateur Role { get; set; }
    public bool Actif { get; set; } = true;
}
