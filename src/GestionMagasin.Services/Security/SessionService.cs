using GestionMagasin.Core.Models;

namespace GestionMagasin.Services.Security;

public sealed class SessionService
{
    private readonly object _lock = new();
    private Utilisateur? _current;

    public Utilisateur? Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public void Login(Utilisateur user)
    {
        lock (_lock)
        {
            _current = user;
        }
    }

    public void Logout()
    {
        lock (_lock)
        {
            _current = null;
        }
    }

    public Utilisateur RequireUser()
    {
        if (Current is null)
            throw new UnauthorizedAccessException("Aucune session active.");

        return Current;
    }
}
