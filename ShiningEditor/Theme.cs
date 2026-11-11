using System.Drawing.Drawing2D;
using ShiningEditor.Game;

namespace ShiningEditor;

/// <summary>
/// Palette taken from the Genesis Shining games' menus: deep navy windows with a
/// silver outer frame and a steel-blue inner frame, white text, and the gold of the
/// "Shining" logo for headings and money. Each game adds its own accent colour
/// (name plate, title plate, highlights).
/// </summary>
internal static class Theme
{
    public static readonly Color Void       = Color.FromArgb(0x05, 0x08, 0x1A); // outer backdrop
    public static readonly Color Window     = Color.FromArgb(0x0B, 0x15, 0x3A); // menu windows
    public static readonly Color WindowHi   = Color.FromArgb(0x13, 0x22, 0x52); // cards / tab pages
    public static readonly Color Field      = Color.FromArgb(0x1B, 0x2E, 0x68); // numeric fields
    public static readonly Color FieldRO    = Color.FromArgb(0x10, 0x1C, 0x44); // read-only fields
    public static readonly Color Silver     = Color.FromArgb(0xE2, 0xE8, 0xF8); // outer window frame
    public static readonly Color Steel      = Color.FromArgb(0x55, 0x78, 0xD2); // inner window frame
    public static readonly Color SteelDeep  = Color.FromArgb(0x2C, 0x44, 0x8C);
    public static readonly Color Gold       = Color.FromArgb(0xF6, 0xC8, 0x3C); // "Shining" logo gold
    public static readonly Color GoldDeep   = Color.FromArgb(0xB0, 0x7C, 0x10);
    public static readonly Color Text       = Color.FromArgb(0xF6, 0xF8, 0xFF);
    public static readonly Color TextMuted  = Color.FromArgb(0x96, 0xA6, 0xD0);
    public static readonly Color HpBar      = Color.FromArgb(0x58, 0xD8, 0x48); // battle HP gauge
    public static readonly Color MpBar      = Color.FromArgb(0x48, 0xB4, 0xF8); // battle MP gauge
    public static readonly Color BarEmpty   = Color.FromArgb(0x26, 0x34, 0x66);
    public static readonly Color Edited     = Color.FromArgb(0xFF, 0x9C, 0x3C); // unsaved-change marker

    /// <summary>Accent of the game currently being edited.</summary>
    public static GameAccent Accent { get; set; } =
        new(Color.FromArgb(0x2E, 0x5C, 0xD6), Color.FromArgb(0x0C, 0x1E, 0x66), Color.FromArgb(0x9C, 0xC4, 0xFF));

    public static readonly string Family = "Segoe UI";

    public static Font Body(float size, FontStyle style = FontStyle.Regular) => new(Family, size, style);

    public static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var p = new GraphicsPath();
        int d = Math.Max(2, radius * 2);
        p.AddArc(r.Left, r.Top, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    /// <summary>
    /// Paints a Shining-style menu window: navy gradient fill, a silver outer frame
    /// and a steel-blue inner frame, with softly rounded corners.
    /// </summary>
    public static void PaintWindow(Graphics g, Rectangle r, Color top, Color bottom, Color outer, Color inner, int radius = 8)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        r.Width -= 1;
        r.Height -= 1;
        if (r.Width < 8 || r.Height < 8)
        {
            return;
        }

        using (var path = Rounded(r, radius))
        using (var fill = new LinearGradientBrush(r, top, bottom, LinearGradientMode.Vertical))
        {
            g.FillPath(fill, path);
            using var pen = new Pen(outer, 2f);
            g.DrawPath(pen, path);
        }

        var ir = Rectangle.Inflate(r, -4, -4);
        using (var ipath = Rounded(ir, Math.Max(2, radius - 3)))
        using (var ipen = new Pen(inner, 1.5f))
        {
            g.DrawPath(ipen, ipath);
        }
    }

    public static void PaintWindow(Graphics g, Rectangle r, int radius = 8) =>
        PaintWindow(g, r, WindowHi, Window, Silver, Steel, radius);

    /// <summary>Applies the palette to a simple dialog (About, Error Log).</summary>
    public static void ApplyDialog(Control root)
    {
        root.BackColor = Window;
        root.ForeColor = Text;
        foreach (Control c in root.Controls)
        {
            switch (c)
            {
                case TextBoxBase tb:
                    tb.BackColor = Void;
                    tb.ForeColor = Text;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case Button b:
                    StyleButton(b);
                    break;
                case PictureBox:
                    break;
                default:
                    c.BackColor = Window;
                    c.ForeColor = Text;
                    break;
            }

            if (c.HasChildren && c is not TextBoxBase)
            {
                ApplyDialog(c);
            }
        }
    }

    public static void StyleButton(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Accent.PlateBottom;
        b.ForeColor = Text;
        b.Font = Body(10f, FontStyle.Bold);
        b.FlatAppearance.BorderColor = Silver;
        b.FlatAppearance.BorderSize = 2;
        b.FlatAppearance.MouseOverBackColor = Accent.PlateTop;
        b.FlatAppearance.MouseDownBackColor = SteelDeep;
        b.Cursor = Cursors.Hand;
    }
}

/// <summary>Themed colours for the menu strip (navy popups, steel highlight).</summary>
internal sealed class ShiningMenuColors : ProfessionalColorTable
{
    public override Color MenuStripGradientBegin => Theme.Void;
    public override Color MenuStripGradientEnd => Theme.Void;
    public override Color ToolStripDropDownBackground => Theme.Window;
    public override Color ImageMarginGradientBegin => Theme.Window;
    public override Color ImageMarginGradientMiddle => Theme.Window;
    public override Color ImageMarginGradientEnd => Theme.Window;
    public override Color MenuItemSelected => Theme.SteelDeep;
    public override Color MenuItemSelectedGradientBegin => Theme.SteelDeep;
    public override Color MenuItemSelectedGradientEnd => Theme.SteelDeep;
    public override Color MenuItemBorder => Theme.Steel;
    public override Color MenuItemPressedGradientBegin => Theme.Field;
    public override Color MenuItemPressedGradientEnd => Theme.Window;
    public override Color MenuBorder => Theme.Steel;
    public override Color SeparatorDark => Theme.Steel;
    public override Color CheckBackground => Theme.SteelDeep;
    public override Color CheckSelectedBackground => Theme.Steel;
    public override Color CheckPressedBackground => Theme.Steel;
}
