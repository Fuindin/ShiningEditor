using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ShiningEditor;

/// <summary>
/// A fully custom-drawn tab control: a themed tab strip on top and plain panels as
/// pages. Replaces the Win32 TabControl, whose strip and page border can't be
/// restyled reliably and stay white.
/// </summary>
internal sealed class ShiningTabControl : Control
{
    private readonly List<(string Title, Control Page)> _pages = new();
    private readonly List<Rectangle> _tabRects = new();
    private int _selected = -1;

    public ShiningTabControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    public event EventHandler? SelectedIndexChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selected;
        set => SelectPage(value);
    }

    private int StripHeight => TextRenderer.MeasureText("Ag", Font).Height + LogicalToDeviceUnits(16);
    private int FrameWidth => Math.Max(2, LogicalToDeviceUnits(2));

    /// <summary>Replaces the pages shown by the control.</summary>
    public void SetPages(params (string Title, Control Page)[] pages)
    {
        SuspendLayout();
        Controls.Clear();
        _pages.Clear();
        foreach (var p in pages)
        {
            _pages.Add(p);
            p.Page.Visible = false;
            p.Page.Dock = DockStyle.None;
            Controls.Add(p.Page);
        }

        _selected = -1;
        ResumeLayout(false);
        SelectPage(_pages.Count > 0 ? 0 : -1);
        PerformLayout();
        Invalidate();
    }

    private void SelectPage(int index)
    {
        if (index < -1 || index >= _pages.Count || index == _selected)
        {
            return;
        }

        _selected = index;
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Page.Visible = i == index;
        }

        Invalidate();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        int f = FrameWidth;
        var r = new Rectangle(f, StripHeight + f, Math.Max(0, Width - 2 * f), Math.Max(0, Height - StripHeight - 2 * f));
        foreach (var (_, page) in _pages)
        {
            page.Bounds = r;
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        PerformLayout();
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        for (int i = 0; i < _tabRects.Count; i++)
        {
            if (_tabRects[i].Contains(e.Location))
            {
                SelectPage(i);
                return;
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int strip = StripHeight;
        int f = FrameWidth;

        // Page frame (steel, like the inner border of the game's windows).
        var frame = new Rectangle(f / 2, strip + f / 2, Width - f, Height - strip - f);
        using (var pen = new Pen(Theme.Steel, f))
        {
            g.DrawRectangle(pen, frame);
        }

        _tabRects.Clear();
        int x = 0;
        int minW = LogicalToDeviceUnits(120);
        for (int i = 0; i < _pages.Count; i++)
        {
            int w = Math.Max(minW, TextRenderer.MeasureText(_pages[i].Title, Font).Width + LogicalToDeviceUnits(40));
            var rect = new Rectangle(x, 0, w, strip);
            _tabRects.Add(rect);
            DrawTab(g, rect, _pages[i].Title, i == _selected);
            x += w + LogicalToDeviceUnits(4);
        }
    }

    private void DrawTab(Graphics g, Rectangle rect, string title, bool selected)
    {
        var plate = Rectangle.Inflate(rect, -1, 0);
        plate.Height -= 1;
        if (plate.Width <= 0 || plate.Height <= 0)
        {
            return;
        }

        using (var path = Theme.Rounded(plate, LogicalToDeviceUnits(6)))
        using (var grad = new LinearGradientBrush(plate,
                   selected ? Theme.Accent.PlateTop : Theme.WindowHi,
                   selected ? Theme.Accent.PlateBottom : Theme.Window, LinearGradientMode.Vertical))
        using (var pen = new Pen(selected ? Theme.Silver : Theme.SteelDeep, selected ? 2 : 1))
        {
            g.FillPath(grad, path);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, title, Font, rect,
            selected ? Theme.Gold : Theme.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
