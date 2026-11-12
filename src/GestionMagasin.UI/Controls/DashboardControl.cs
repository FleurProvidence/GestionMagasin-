using GestionMagasin.Services.Reporting;
using GestionMagasin.UI.Controls;

namespace GestionMagasin.UI.Controls;

public sealed class DashboardControl : UserControl
{
    private readonly DashboardService _dashboardService;
    private readonly Label _dateLabel = new();
    private readonly FlowLayoutPanel _cards = new();

    public DashboardControl(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        Dock = DockStyle.Fill;
        BackColor = DesignSystem.AppBackground;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _dateLabel.Dock = DockStyle.Fill;
        _dateLabel.Font = DesignSystem.BodyFont(11f, FontStyle.Bold);
        _dateLabel.ForeColor = Color.FromArgb(71, 85, 105);

        _cards.Dock = DockStyle.Fill;
        _cards.WrapContents = true;
        _cards.FlowDirection = FlowDirection.LeftToRight;

        root.Controls.Add(_dateLabel, 0, 0);
        root.Controls.Add(_cards, 0, 1);
        Controls.Add(root);

        Load += async (_, _) => await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            var snapshot = await _dashboardService.ChargerAsync(DateOnly.FromDateTime(DateTime.Today));
            _dateLabel.Text = $"Date métier : {snapshot.DateMetier:dd/MM/yyyy}";

            _cards.Controls.Clear();

            var cards = new[]
            {
                new StatCard { Title = "Chiffre d'affaires du jour", Value = $"{snapshot.ChiffreAffairesJour:N0} FCFA", Detail = "Ventes validées", Accent = DesignSystem.Primary },
                new StatCard { Title = "Ventes validées", Value = snapshot.NombreVentesJour.ToString("N0"), Detail = "Transactions", Accent = DesignSystem.Primary },
                new StatCard { Title = "Unités vendues", Value = snapshot.UnitesVenduesJour.ToString("N0"), Detail = "Quantités", Accent = DesignSystem.Primary },
                new StatCard { Title = "Références en rupture", Value = snapshot.ReferencesEnRupture.ToString("N0"), Detail = "Produits à 0", Accent = snapshot.ReferencesEnRupture > 0 ? DesignSystem.DangerText : DesignSystem.Primary },
                new StatCard { Title = "Stock faible", Value = snapshot.ReferencesStockFaible.ToString("N0"), Detail = "Sous seuil d’alerte", Accent = snapshot.ReferencesStockFaible > 0 ? DesignSystem.WarningText : DesignSystem.Primary }
            };

            foreach (var card in cards)
            {
                _cards.Controls.Add(card);
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Session invalide ou expirée.");
        }
    }
}
