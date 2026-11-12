namespace GestionMagasin.UI;

public sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "GestionMagasin";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1366, 768);
        BackColor = Color.FromArgb(245, 247, 250);

        var title = new Label
        {
            Text = "GestionMagasin",
            Dock = DockStyle.Top,
            Height = 52,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(20, 10, 0, 0)
        };

        Controls.Add(title);
    }
}
