using GestionMagasin.Data.Persistence;
using GestionMagasin.Services.Products;
using GestionMagasin.Services.Reporting;
using GestionMagasin.Services.Sales;
using GestionMagasin.Services.Security;
using GestionMagasin.Services.Stock;
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

        services.AddSingleton<SqliteProductRepository>();
        services.AddSingleton<SqliteStockRepository>();
        services.AddSingleton<SqliteSaleRepository>();
        services.AddSingleton<SqliteReportingRepository>();

        services.AddSingleton<IProduitRepository>(sp => sp.GetRequiredService<SqliteProductRepository>());
        services.AddSingleton<IStockRepository>(sp => sp.GetRequiredService<SqliteStockRepository>());
        services.AddSingleton<ISaleRepository>(sp => sp.GetRequiredService<SqliteSaleRepository>());
        services.AddSingleton<IReportingRepository>(sp => sp.GetRequiredService<SqliteReportingRepository>());

        services.AddSingleton<AuthService>();
        services.AddSingleton<ProductService>();
        services.AddSingleton<StockService>();
        services.AddSingleton<SaleService>();
        services.AddSingleton<DashboardService>();

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
