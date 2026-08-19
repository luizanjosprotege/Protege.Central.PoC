using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Protege.Central.PoC.App;

/// <summary>
/// Paleta e componentes visuais copiados do design system do app mobile
/// "Protege Follow Me" (C:\proj\app-follow-me\src\theme), adaptados para desktop.
/// </summary>
internal static class Theme
{
    // protege_700 / header navy
    public static readonly Color HeaderNavy = ColorTranslator.FromHtml("#043154");
    public static readonly Color PrimaryNavy = ColorTranslator.FromHtml("#012651");
    public static readonly Color PrimaryNavyHover = ColorTranslator.FromHtml("#0256B6");
    public static readonly Color PageBg = ColorTranslator.FromHtml("#E6EBF0");
    public static readonly Color CardBg = Color.White;
    public static readonly Color Divider = ColorTranslator.FromHtml("#d8dce0");
    public static readonly Color TitleText = ColorTranslator.FromHtml("#043154");
    public static readonly Color SecondaryText = ColorTranslator.FromHtml("#6b7280");
    public static readonly Color CardSelectedBg = ColorTranslator.FromHtml("#EEF3F8");

    public static readonly Color SuccessBg = ColorTranslator.FromHtml("#E3F5E6");
    public static readonly Color SuccessText = ColorTranslator.FromHtml("#1E7B33");
    public static readonly Color NeutralBg = ColorTranslator.FromHtml("#EFEFEF");
    public static readonly Color NeutralText = ColorTranslator.FromHtml("#595959");
    public static readonly Color DangerBg = ColorTranslator.FromHtml("#F7E6E9");
    public static readonly Color DangerText = ColorTranslator.FromHtml("#C0334D");

    private static readonly PrivateFontCollection FontCollection = LoadFonts();
    private static FontFamily Family => FontCollection.Families.Length > 0
        ? FontCollection.Families[0]
        : FontFamily.GenericSansSerif;

    public static Font Regular(float size, FontStyle style = FontStyle.Regular) => new(Family, size, style, GraphicsUnit.Point);
    public static Font Bold(float size) => new(Family, size, FontStyle.Bold, GraphicsUnit.Point);

    private static PrivateFontCollection LoadFonts()
    {
        var collection = new PrivateFontCollection();
        var dir = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        foreach (var file in new[] { "OpenSans-Regular.ttf", "OpenSans-Bold.ttf" })
        {
            var path = Path.Combine(dir, file);
            if (File.Exists(path)) collection.AddFontFile(path);
        }
        return collection;
    }

    public static void RoundCorners(Control control, int radius)
    {
        void Apply()
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            control.Region = new Region(RoundedRectPath(control.ClientRectangle, radius));
        }
        control.Resize += (_, _) => Apply();
        Apply();
    }

    private static GraphicsPath RoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        if (d > rect.Width) d = rect.Width;
        if (d > rect.Height) d = rect.Height;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public enum ButtonKind { Primary, Secondary, Selected }

    public static Button CreateButton(string text, ButtonKind kind = ButtonKind.Secondary)
    {
        var (bg, fg, border) = kind switch
        {
            ButtonKind.Primary => (PrimaryNavy, Color.White, PrimaryNavy),
            ButtonKind.Selected => (CardSelectedBg, HeaderNavy, PrimaryNavyHover),
            _ => (CardBg, TitleText, Divider),
        };

        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 7, 12, 7),
            Margin = new Padding(0, 0, 8, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = bg,
            ForeColor = fg,
            Font = Regular(9.5F, kind == ButtonKind.Selected ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = border;
        btn.FlatAppearance.MouseOverBackColor = kind == ButtonKind.Primary ? PrimaryNavyHover : CardSelectedBg;
        RoundCorners(btn, 6);
        return btn;
    }

    public static void SetKind(Button btn, ButtonKind kind)
    {
        var (bg, fg, border) = kind switch
        {
            ButtonKind.Primary => (PrimaryNavy, Color.White, PrimaryNavy),
            ButtonKind.Selected => (CardSelectedBg, HeaderNavy, PrimaryNavyHover),
            _ => (CardBg, TitleText, Divider),
        };
        btn.BackColor = bg;
        btn.ForeColor = fg;
        btn.Font = Regular(9.5F, kind == ButtonKind.Selected ? FontStyle.Bold : FontStyle.Regular);
        btn.FlatAppearance.BorderColor = border;
    }
}
