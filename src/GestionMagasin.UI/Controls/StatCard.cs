using System.Drawing;

namespace GestionMagasin.UI.Controls;

public sealed class StatCard : UserControl
{
    private readonly Label _title;
    private readonly Label _value;
    private readonly Label _detail;

    public event EventHandler? Clicked;

    public StatCard()
    {
        BackColor = DesignSystem.Surface;
        BorderStyle = BorderStyle.FixedSingle;
        Width = 230;
        Height = 120;
        Cursor = Cursors.Hand;

        _title = new Label
        {
            AutoSize = true,
            Font = DesignSystem.BodyFont(),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(16, 12)
        };

        _value = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(16, 38)
        };

        _detail = new Label
        {
            AutoSize = true,
            Font = DesignSystem.BodyFont(9.5f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(16, 84)
        };

        Controls.Add(_title);
        Controls.Add(_value);
        Controls.Add(_detail);

        Click += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        DoubleClick += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
    }

    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    public string Value
    {
        get => _value.Text;
        set => _value.Text = value;
    }

    public string Detail
    {
        get => _detail.Text;
        set => _detail.Text = value;
    }

    public Color Accent { get; set; } = DesignSystem.Primary;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Accent, 3);
        e.Graphics.DrawLine(pen, 0, 5, 0, Height - 5);
    }
}
