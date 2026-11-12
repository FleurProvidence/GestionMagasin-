# GestionMagasin

Application Windows Forms .NET 8 pour la gestion de magasin, avec SQLite native et architecture en couches.

## Structure

- `src/GestionMagasin.Core` : modèles et types de domaine
- `src/GestionMagasin.Data` : SQLite et migrations
- `src/GestionMagasin.Services` : logique métier et sécurité
- `src/GestionMagasin.UI` : interface utilisateur WinForms

## Lancer le projet

```powershell
dotnet restore
dotnet build GestionMagasin.sln
dotnet run --project src/GestionMagasin.UI/GestionMagasin.UI.csproj
```

## Base de données

La base SQLite est créée dans :

```text
%LocalAppData%\GestionMagasin\data.db
```

## Sécurité

- Mot de passe hashé par PBKDF2 + SHA-256
- `PRAGMA foreign_keys = ON`
- session utilisateur
- premier compte administrateur automatique si la base est vide
