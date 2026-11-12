using GestionMagasin.Data.Persistence;
using GestionMagasin.Services.Security;
using Microsoft.Extensions.DependencyInjection;

namespace GestionMagasin.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();

        var factory = SqliteConnectionFactory.ForLocalAppData();
        services.AddSingleton(factory);
        services.AddSingleton(new DatabaseMigrator(factory));
        services.AddSingleton(new PasswordHasher());
        services.AddSingleton<SessionService>();
        services.AddSingleton<AuthService>();

        var provider = services.BuildServiceProvider();

        var migrator = provider.GetRequiredService<DatabaseMigrator>();
        migrator.Migrate();

        var auth = provider.GetRequiredService<AuthService>();
        if (auth.IsFirstLaunch())
        {
            using var form = new FirstAdminSetupForm(auth);
            if (form.ShowDialog() != DialogResult.OK)
                return;
        }

        Application.Run(new MainForm());
    }
}
