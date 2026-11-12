namespace GestionMagasin.UI.Controls;

public static class DesignSystem
{
    public static readonly System.Drawing.Color AppBackground = System.Drawing.ColorTranslator.FromHtml("#F5F7FA");
    public static readonly System.Drawing.Color Surface = System.Drawing.ColorTranslator.FromHtml("#FFFFFF");
    public static readonly System.Drawing.Color Border = System.Drawing.ColorTranslator.FromHtml("#E2E8F0");
    public static readonly System.Drawing.Color Sidebar = System.Drawing.ColorTranslator.FromHtml("#1E293B");
    public static readonly System.Drawing.Color SidebarText = System.Drawing.ColorTranslator.FromHtml("#F8FAFC");
    public static readonly System.Drawing.Color Primary = System.Drawing.ColorTranslator.FromHtml("#2563EB");
    public static readonly System.Drawing.Color PrimaryHover = System.Drawing.ColorTranslator.FromHtml("#1D4ED8");
    public static readonly System.Drawing.Color SuccessText = System.Drawing.ColorTranslator.FromHtml("#15803D");
    public static readonly System.Drawing.Color SuccessBack = System.Drawing.ColorTranslator.FromHtml("#DCFCE7");
    public static readonly System.Drawing.Color WarningText = System.Drawing.ColorTranslator.FromHtml("#B45309");
    public static readonly System.Drawing.Color WarningBack = System.Drawing.ColorTranslator.FromHtml("#FEF3C7");
    public static readonly System.Drawing.Color DangerText = System.Drawing.ColorTranslator.FromHtml("#B91C1C");
    public static readonly System.Drawing.Color DangerBack = System.Drawing.ColorTranslator.FromHtml("#FEE2E2");

    public static System.Drawing.Font BodyFont(float size = 10.5f, System.Drawing.FontStyle style = System.Drawing.FontStyle.Regular)
        => new("Segoe UI", size, style, System.Drawing.GraphicsUnit.Point);

    public static System.Drawing.Font TitleFont(float size = 18f)
        => new("Segoe UI", size, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
}
