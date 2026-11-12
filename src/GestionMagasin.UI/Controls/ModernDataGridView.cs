namespace GestionMagasin.UI.Controls;

public sealed class ModernDataGridView : DataGridView
{
    public ModernDataGridView()
    {
        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.None;
        BackgroundColor = DesignSystem.Surface;
        AutoGenerateColumns = false;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        ReadOnly = true;
        RowHeadersVisible = false;
        MultiSelect = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        EnableHeadersVisualStyles = false;
        ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
        ColumnHeadersDefaultCellStyle.Font = DesignSystem.BodyFont(9.5f, FontStyle.Bold);
        DefaultCellStyle.Font = DesignSystem.BodyFont(9.5f);
        DefaultCellStyle.BackColor = DesignSystem.Surface;
        DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
    }
}
