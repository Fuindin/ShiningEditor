using System.Drawing.Drawing2D;
using System.Reflection;
using ShiningEditor.Game;

namespace ShiningEditor;

public sealed partial class MainForm : Form
{
    private GameDefinition _game = Games.All[1];      // Shining Force by default
    private SaveState? _save;
    private readonly List<CharacterDef> _chars = new();
    private readonly Dictionary<string, Control> _fieldEditors = new();
    private readonly Dictionary<string, Label> _fieldHints = new();
    private readonly ToolTip _tips = new() { AutoPopDelay = 8000, InitialDelay = 400 };

    // Unsaved edits: character names (plus "Gold") changed since the last save.
    // Edits are written straight into the in-memory save, so switching between
    // characters never loses them — Save writes all of them in one go.
    private readonly HashSet<string> _edited = new(StringComparer.Ordinal);
    private const string GoldKey = "\u0001gold";

    private CharacterDef? _bound;       // character whose values are in the editors
    private bool _loading;              // suppress write-back while populating controls
    private bool _switchingGame;

    private CharacterDef? Current =>
        _lstChars.SelectedIndex is int i && i >= 0 && i < _chars.Count ? _chars[i] : null;

    private bool Dirty => _edited.Count > 0;

    public MainForm()
    {
        InitializeComponent();   // visual layout (MainForm.Designer.cs)
        ApplyTheme();
        BuildGameMenu();
        WireEvents();
        SelectGame(_game, force: true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Size the window here, after WinForms has applied DPI auto-scaling.
        FitToScreen();
        CenterToScreen();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateListMetrics();
        FitFieldTable();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        UpdateListMetrics();
        FitFieldTable();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!e.Cancel && !ConfirmDiscardOrSave("closing"))
        {
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }

    // ---- Metrics ----------------------------------------------------------------

    /// <summary>Opens at a comfortable size for the current monitor and DPI.</summary>
    private void FitToScreen()
    {
        var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
        int w = Math.Min(wa.Width * 92 / 100, LogicalToDeviceUnits(1320));
        int h = Math.Min(wa.Height * 92 / 100, LogicalToDeviceUnits(860));
        Size = new Size(Math.Max(w, MinimumSize.Width), Math.Max(h, MinimumSize.Height));
    }

    /// <summary>
    /// Sizes the STATUS table to exactly the page width (the description column
    /// absorbs the slack) and lets the page scroll vertically only.
    /// </summary>
    private void FitFieldTable()
    {
        var page = _tabStatus;
        int w = page.ClientSize.Width - page.Padding.Horizontal;
        if (w <= 0)
        {
            return;
        }

        int h = _fieldTable.GetPreferredSize(new Size(w, 0)).Height;
        _fieldTable.SetBounds(page.Padding.Left, page.Padding.Top + page.AutoScrollPosition.Y, w, h);
        page.AutoScrollMinSize = new Size(0, h + page.Padding.Vertical);
    }

    private void UpdateListMetrics()
    {
        if (!_lstChars.IsHandleCreated)
        {
            return;
        }

        using var g = _lstChars.CreateGraphics();
        using var small = Theme.Body(9f, FontStyle.Bold);
        float name = _lstChars.Font.GetHeight(g), line = small.GetHeight(g);
        _lstChars.ItemHeight = Math.Min(255, (int)Math.Ceiling(name + line * 2 + 36));
        _itemList.ItemHeight = Math.Min(255, (int)Math.Ceiling(_itemList.Font.GetHeight(g)) + 14);
        _spellList.ItemHeight = _itemList.ItemHeight;

        // Name plate: tall enough for the name and the subtitle line at this DPI.
        using var nameFont = Theme.Body(20f, FontStyle.Bold);
        using var subFont = Theme.Body(10.5f, FontStyle.Italic);
        _namePlate.Height = (int)Math.Ceiling(nameFont.GetHeight(g) + subFont.GetHeight(g)) + LogicalToDeviceUnits(34);

        _lstChars.Invalidate();
    }

    // ---- Theme ------------------------------------------------------------------

    private void ApplyTheme()
    {
        BackColor = Theme.Void;
        ForeColor = Theme.Text;
        Font = Theme.Body(9.75f);

        _menu.Renderer = new ToolStripProfessionalRenderer(new ShiningMenuColors()) { RoundedEdges = false };
        _menu.BackColor = Theme.Void;
        _menu.Font = Theme.Body(9.75f);
        StyleMenuItems(_menu.Items);

        _header.BackColor = Theme.Window;
        _header.Paint += (_, e) =>
        {
            // Silver rule along the bottom edge, like the frame under a Shining message window.
            using var pen = new Pen(Theme.Steel, 2);
            e.Graphics.DrawLine(pen, 0, _header.Height - 1, _header.Width, _header.Height - 1);
        };
        _lblFile.ForeColor = Theme.Text;
        _lblInfo.ForeColor = Theme.TextMuted;
        _lblFile.BackColor = _lblInfo.BackColor = Color.Transparent;

        _lblTitle.BackColor = Theme.Window;
        _lblTitle.Font = Theme.Body(15f, FontStyle.Bold | FontStyle.Italic);
        _lblTitle.Paint += PaintTitlePlate;

        _goldPanel.BackColor = Theme.Window;
        foreach (var l in new[] { _lblGame, _lblGold })
        {
            l.ForeColor = Theme.Gold;
            l.BackColor = Color.Transparent;
            l.Font = Theme.Body(12f, FontStyle.Bold);
        }

        _cboGame.BackColor = Theme.Field;
        _cboGame.ForeColor = Theme.Text;
        _cboGame.Font = Theme.Body(11f, FontStyle.Bold);
        _numGold.Font = Theme.Body(12f, FontStyle.Bold);
        StyleNumeric(_numGold);

        _lblBanner.Font = Theme.Body(9.5f, FontStyle.Bold);

        _leftPanel.BackColor = Theme.Window;
        StyleHeader(_lblParty);
        _lstChars.BackColor = Theme.Window;
        _lstChars.ForeColor = Theme.Text;
        _lstChars.Font = Theme.Body(12f, FontStyle.Bold);

        _divider.BackColor = Theme.Steel;

        _coverPanel.BackColor = Theme.Window;
        _picCover.BackColor = Theme.Void;
        _coverPanel.Paint += PaintCoverPanel;
        _coverPanel.Resize += (_, _) =>
        {
            SizeCover();
            _coverPanel.Invalidate();
        };

        _rightPanel.BackColor = Theme.Window;
        _namePlate.BackColor = Theme.Window;
        _namePlate.Paint += PaintNamePlate;
        _namePlate.Resize += (_, _) => _namePlate.Invalidate();

        _tabs.Font = Theme.Body(10f, FontStyle.Bold);
        foreach (var page in new[] { _tabStatus, _tabItems, _tabMagic })
        {
            page.BackColor = Theme.WindowHi;
        }

        _tabStatus.Resize += (_, _) => FitFieldTable();

        _fieldTable.BackColor = Theme.WindowHi;
        foreach (var l in new[] { _lblItemsHdr, _lblMagicHdr })
        {
            StyleHeader(l);
            l.BackColor = Theme.WindowHi;
        }

        foreach (var l in new[] { _itemsNote, _magicNote })
        {
            l.BackColor = Theme.WindowHi;
            l.ForeColor = Theme.TextMuted;
            l.Font = Theme.Body(8.5f, FontStyle.Italic);
        }

        foreach (var lb in new[] { _itemList, _spellList })
        {
            lb.BackColor = Theme.Window;
            lb.ForeColor = Theme.Text;
            lb.Font = Theme.Body(11f);
        }

        ApplyAccent();
    }

    /// <summary>Re-colours the parts that follow the current game's accent.</summary>
    private void ApplyAccent()
    {
        Theme.StyleButton(_btnOpen);
        Theme.StyleButton(_btnSave);
        _btnSave.BackColor = Theme.GoldDeep;
        _btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xD0, 0x98, 0x20);
        _btnSave.FlatAppearance.BorderColor = Theme.Gold;

        _lblTitle.Invalidate();
        _namePlate.Invalidate();
        _coverPanel.Invalidate();
        _lstChars.Invalidate();
        _tabs.Invalidate();
    }

    private static void StyleMenuItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.ForeColor = Theme.Text;
            if (item is ToolStripMenuItem mi && mi.HasDropDownItems)
            {
                StyleMenuItems(mi.DropDownItems);
            }
        }
    }

    private static void StyleHeader(Label l)
    {
        l.ForeColor = Theme.Gold;
        l.BackColor = Color.Transparent;
        l.Font = Theme.Body(12f, FontStyle.Bold | FontStyle.Italic);
        l.Padding = new Padding(2, 4, 0, 0);
    }

    private static void StyleNumeric(NumericUpDown n)
    {
        n.BorderStyle = BorderStyle.FixedSingle;
        n.BackColor = Theme.Field;
        n.ForeColor = Theme.Text;
    }

    // ---- Wiring -----------------------------------------------------------------

    private void BuildGameMenu()
    {
        _mnuGame.DropDownItems.Clear();
        _cboGame.Items.Clear();
        foreach (var g in Games.All)
        {
            var item = new ToolStripMenuItem(g.MenuText) { Tag = g, ForeColor = Theme.Text };
            item.Click += (_, _) => SelectGame(g);
            _mnuGame.DropDownItems.Add(item);
            _cboGame.Items.Add(g.Title);
        }
    }

    private void WireEvents()
    {
        _mnuOpen.Click += (_, _) => OpenFile();
        _btnOpen.Click += (_, _) => OpenFile();
        _mnuSave.Click += (_, _) => SaveFile();
        _btnSave.Click += (_, _) => SaveFile();
        _mnuReload.Click += (_, _) => ReloadFromDisk();
        _mnuExit.Click += (_, _) => Close();
        _mnuInstructions.Click += (_, _) => ShowInstructions();
        _mnuViewLog.Click += (_, _) =>
        {
            using var view = new ErrorLogView();
            view.ShowDialog(this);
        };
        _mnuClearLog.Click += (_, _) => ClearErrorLog();
        _mnuAbout.Click += (_, _) =>
        {
            using var about = new AboutForm();
            about.ShowDialog(this);
        };

        _cboGame.SelectedIndexChanged += (_, _) =>
        {
            if (!_switchingGame && _cboGame.SelectedIndex >= 0)
            {
                SelectGame(Games.All[_cboGame.SelectedIndex]);
            }
        };

        _numGold.ValueChanged += (_, _) =>
        {
            if (_loading || _save is null)
            {
                return;
            }

            if (_save.Write(_game.GoldOffset, _game.GoldWidth, (long)_numGold.Value))
            {
                MarkEdited(GoldKey);
            }
        };

        _lstChars.DrawItem += DrawCharacterCard;
        _lstChars.SelectedIndexChanged += (_, _) => LoadSelectedCharacter();
        _itemList.DrawItem += (s, e) => DrawSlotRow(_itemList, e, "◆");
        _spellList.DrawItem += (s, e) => DrawSlotRow(_spellList, e, "✦");

        // Drag events don't bubble from child controls to the form, and the form's
        // surface is entirely covered by docked panels, so every control needs to
        // accept the drop itself.
        EnableFileDrop(this);
    }

    // ---- Drag & drop --------------------------------------------------------------

    private void EnableFileDrop(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += OnFileDragEnter;
        control.DragOver += OnFileDragEnter;
        control.DragDrop += OnFileDragDrop;

        foreach (Control child in control.Controls)
        {
            EnableFileDrop(child);
        }

        // Controls created later (e.g. when switching games) get hooked too.
        control.ControlAdded += (_, e) =>
        {
            if (e.Control is not null)
            {
                EnableFileDrop(e.Control);
            }
        };
    }

    private void OnFileDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnFileDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            var path = files[0];
            // Defer the load so any prompts/message boxes don't freeze Explorer
            // while it waits for the drop to finish.
            BeginInvoke(() =>
            {
                Activate();
                LoadPath(path);
            });
        }
    }

    // ---- Game selection -----------------------------------------------------------

    private void SelectGame(GameDefinition game, bool force = false)
    {
        if (!force && ReferenceEquals(game, _game))
        {
            SyncGameSelectors();
            return;
        }

        if (!force && _save is not null)
        {
            if (!ConfirmDiscardOrSave("switching games"))
            {
                SyncGameSelectors();
                return;
            }

            CloseSave();
        }

        _game = game;
        Theme.Accent = game.Accent;
        SyncGameSelectors();
        BuildFieldEditors();
        LoadCover();

        // Items / magic tabs only for games whose inventory is mapped.
        if (game.HasInventory)
        {
            _tabs.SetPages(("STATUS", _tabStatus), ("ITEM", _tabItems), ("MAGIC", _tabMagic));
        }
        else
        {
            _tabs.SetPages(("STATUS", _tabStatus));
        }

        _numGold.Maximum = SaveState.MaxFor(game.GoldWidth);
        _lblParty.Text = game is ShiningInTheDarknessGame ? "PARTY" : "FORCE MEMBERS";

        ApplyAccent();
        FitFieldTable();
        UpdateState();
    }

    private void SyncGameSelectors()
    {
        _switchingGame = true;
        int idx = IndexOfGame(_game);
        _cboGame.SelectedIndex = idx;
        foreach (ToolStripItem item in _mnuGame.DropDownItems)
        {
            if (item is ToolStripMenuItem mi)
            {
                mi.Checked = ReferenceEquals(mi.Tag, _game);
            }
        }

        _switchingGame = false;
    }

    private static int IndexOfGame(GameDefinition g)
    {
        for (int i = 0; i < Games.All.Count; i++)
        {
            if (ReferenceEquals(Games.All[i], g))
            {
                return i;
            }
        }

        return -1;
    }

    private void LoadCover()
    {
        _picCover.Image?.Dispose();
        _picCover.Image = null;
        try
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(_game.CoverResource);
            if (s is not null)
            {
                using var img = Image.FromStream(s);
                _picCover.Image = new Bitmap(img);
            }
        }
        catch (Exception ex)
        {
            Log.Write($"Could not load cover art '{_game.CoverResource}': {ex.Message}");
        }

        SizeCover();
    }

    /// <summary>Keeps the cover picture box at the image's aspect ratio inside the panel.</summary>
    private void SizeCover()
    {
        int w = _coverPanel.ClientSize.Width - _coverPanel.Padding.Horizontal;
        if (_picCover.Image is { } img && img.Width > 0 && w > 0)
        {
            _picCover.Height = Math.Min((int)(w * (double)img.Height / img.Width),
                Math.Max(120, _coverPanel.ClientSize.Height - _coverPanel.Padding.Vertical - 110));
        }
    }

    // ---- Owner drawing ------------------------------------------------------------

    private void PaintTitlePlate(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        var r = _lblTitle.ClientRectangle;
        g.Clear(Theme.Window);
        Theme.PaintWindow(g, Rectangle.Inflate(r, -3, -3), Theme.Accent.PlateTop, Theme.Accent.PlateBottom,
            Theme.Silver, Theme.Gold, 10);

        // Gold, slightly embossed lettering — a nod to the "Shining" logo.
        var shadow = r;
        shadow.Offset(2, 2);
        const TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(g, _lblTitle.Text, _lblTitle.Font, shadow, Theme.GoldDeep, flags);
        TextRenderer.DrawText(g, _lblTitle.Text, _lblTitle.Font, r, Theme.Gold, flags);
    }

    private void PaintCoverPanel(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        var frame = Rectangle.Inflate(_picCover.Bounds, 8, 8);
        Theme.PaintWindow(g, frame, Theme.Void, Theme.Void, Theme.Silver, Theme.Accent.Glow, 10);

        // Caption window under the box art.
        var cap = new Rectangle(frame.X, frame.Bottom + 14, frame.Width, 92);
        if (cap.Bottom > _coverPanel.ClientSize.Height)
        {
            return;
        }

        Theme.PaintWindow(g, cap);
        using var titleFont = Theme.Body(12f, FontStyle.Bold | FontStyle.Italic);
        using var smallFont = Theme.Body(9f);
        var inner = Rectangle.Inflate(cap, -14, -10);
        TextRenderer.DrawText(g, _game.Title, titleFont, inner, Theme.Gold,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        string console = _game.IsSegaCd ? "Sega CD save state" : "Genesis save state";
        TextRenderer.DrawText(g, $"Kega Fusion · {console}", smallFont, inner, Theme.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.NoPrefix);
    }

    /// <summary>The name window from the status screen: accent plate with name, subtitle and level.</summary>
    private void PaintNamePlate(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Window);
        var c = Current;
        var r = Rectangle.Inflate(_namePlate.ClientRectangle, -3, -6);
        r.Height -= 8;

        if (_save is null || c is null)
        {
            Theme.PaintWindow(g, r);
            using var hint = Theme.Body(12f, FontStyle.Italic);
            string msg = _save is null
                ? $"Open a {_game.Title} save state to begin  (File ▸ Open, or drag a file here)"
                : "Choose a force member on the left.";
            TextRenderer.DrawText(g, msg, hint, r, Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        Theme.PaintWindow(g, r, Theme.Accent.PlateTop, Theme.Accent.PlateBottom, Theme.Silver, Theme.Accent.Glow, 12);

        using var nameFont = Theme.Body(20f, FontStyle.Bold);
        using var subFont = Theme.Body(10.5f, FontStyle.Italic);
        using var lvFont = Theme.Body(16f, FontStyle.Bold);
        int nameH = (int)Math.Ceiling(nameFont.GetHeight(g));
        var nameRect = new Rectangle(r.X + 22, r.Y + 8, r.Width - 40, nameH);
        TextRenderer.DrawText(g, c.Name, nameFont, nameRect, Theme.Text, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        var subRect = new Rectangle(r.X + 24, nameRect.Bottom, r.Width - 40, r.Bottom - nameRect.Bottom - 6);
        TextRenderer.DrawText(g, _game.Subtitle(_save, c), subFont, subRect, Theme.Silver,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix);

        string lv = $"LV {_game.Get(_save, c, "level") ?? 0}";
        TextRenderer.DrawText(g, lv, lvFont, new Rectangle(r.X, r.Y, r.Width - 28, r.Height), Theme.Gold,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
    }

    /// <summary>Force member card: name, LV, HP and MP gauges like the battle status window.</summary>
    private void DrawCharacterCard(object? sender, DrawItemEventArgs e)
    {
        var g = e.Graphics;
        using (var bg = new SolidBrush(Theme.Window))
        {
            g.FillRectangle(bg, e.Bounds);
        }

        if (_save is null || e.Index < 0 || e.Index >= _chars.Count)
        {
            return;
        }

        var c = _chars[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        bool edited = _edited.Contains(c.Name);
        var card = Rectangle.Inflate(e.Bounds, -4, -5);
        if (selected)
        {
            Theme.PaintWindow(g, card, Theme.Accent.PlateTop, Theme.Accent.PlateBottom, Theme.Silver, Theme.Accent.Glow, 9);
        }
        else
        {
            Theme.PaintWindow(g, card, Theme.WindowHi, Theme.Window, Theme.SteelDeep, Theme.Window, 9);
        }

        using var small = Theme.Body(9f, FontStyle.Bold);
        int nameH = (int)Math.Ceiling(_lstChars.Font.GetHeight(g));
        int lineH = (int)Math.Ceiling(small.GetHeight(g));
        int x = card.X + 16, w = card.Width - 32;

        var nameRect = new Rectangle(x, card.Y + 7, w, nameH);
        TextRenderer.DrawText(g, c.Name, _lstChars.Font, nameRect, Theme.Text, TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        if (edited)
        {
            int nw = TextRenderer.MeasureText(g, c.Name, _lstChars.Font, Size.Empty, TextFormatFlags.NoPrefix).Width;
            TextRenderer.DrawText(g, "● edited", small, new Rectangle(x + nw + 4, nameRect.Y, w - nw, nameRect.Height),
                Theme.Edited, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        TextRenderer.DrawText(g, $"LV {_game.Get(_save, c, "level") ?? 0}", _lstChars.Font, nameRect, Theme.Gold,
            TextFormatFlags.Right | TextFormatFlags.NoPrefix);

        int y = nameRect.Bottom + 4;
        DrawBar(g, small, "HP", _game.Get(_save, c, "hpCur"), _game.Get(_save, c, "hpMax"), Theme.HpBar,
            new Rectangle(x, y, w, lineH + 6));
        DrawBar(g, small, "MP", _game.Get(_save, c, "mpCur"), _game.Get(_save, c, "mpMax"), Theme.MpBar,
            new Rectangle(x, y + lineH + 8, w, lineH + 6));
    }

    private static void DrawBar(Graphics g, Font f, string label, long? cur, long? max, Color color, Rectangle r)
    {
        int labelW = TextRenderer.MeasureText("MP  ", f).Width;
        if (cur is null || max is null)
        {
            TextRenderer.DrawText(g, label, f, new Rectangle(r.X, r.Y, labelW, r.Height - 4), Theme.SteelDeep,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "—", f, new Rectangle(r.X, r.Y, r.Width, r.Height - 4), Theme.SteelDeep,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            return;
        }

        TextRenderer.DrawText(g, label, f, new Rectangle(r.X, r.Y, labelW, r.Height - 4), Theme.Silver,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        var bar = new Rectangle(r.X + labelW, r.Bottom - 6, r.Width - labelW, 4);
        using (var empty = new SolidBrush(Theme.BarEmpty))
        {
            g.FillRectangle(empty, bar);
        }

        if (max > 0)
        {
            int fw = (int)(bar.Width * Math.Clamp((double)cur.Value / max.Value, 0, 1));
            using var fill = new SolidBrush(color);
            g.FillRectangle(fill, bar.X, bar.Y, fw, bar.Height);
        }

        TextRenderer.DrawText(g, $"{cur} / {max}", f, new Rectangle(r.X, r.Y, r.Width, r.Height - 4), Theme.Text,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
    }

    private static void DrawSlotRow(ListBox list, DrawItemEventArgs e, string glyph)
    {
        var g = e.Graphics;
        using (var bg = new SolidBrush(Theme.Window))
        {
            g.FillRectangle(bg, e.Bounds);
        }

        if (e.Index < 0 || e.Index >= list.Items.Count)
        {
            return;
        }

        var r = Rectangle.Inflate(e.Bounds, -12, 0);
        if (list.Items[e.Index] is not SlotEntry s)
        {
            TextRenderer.DrawText(g, list.Items[e.Index]?.ToString() ?? "", list.Font, r, Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        // Thin steel rule between rows, like the item window's slots.
        using (var pen = new Pen(Color.FromArgb(60, Theme.Steel)))
        {
            g.DrawLine(pen, e.Bounds.Left + 8, e.Bounds.Bottom - 1, e.Bounds.Right - 8, e.Bounds.Bottom - 1);
        }

        Color color = s.Empty || s.Unknown ? Theme.TextMuted : s.Equipped ? Theme.Gold : Theme.Text;
        string text = s.Empty ? "— empty —" : s.Name;
        int glyphW = TextRenderer.MeasureText(glyph + " ", list.Font).Width;
        TextRenderer.DrawText(g, glyph, list.Font, new Rectangle(r.X, r.Y, glyphW, r.Height),
            s.Empty ? Theme.SteelDeep : Theme.Accent.Glow, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, text, list.Font, new Rectangle(r.X + glyphW, r.Y, r.Width - glyphW, r.Height), color,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        using var small = Theme.Body(8.5f, FontStyle.Italic);
        string tag = s.Equipped ? $"equipped   0x{s.Raw:X2}" : $"0x{s.Raw:X2}";
        TextRenderer.DrawText(g, tag, small, r, s.Equipped ? Theme.Gold : Theme.TextMuted,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
    }

    // ---- Stat rows ----------------------------------------------------------------

    private void BuildFieldEditors()
    {
        _fieldTable.SuspendLayout();
        foreach (Control ctl in _fieldTable.Controls.Cast<Control>().ToList())
        {
            ctl.Dispose();
        }

        _fieldTable.Controls.Clear();
        _fieldEditors.Clear();
        _fieldHints.Clear();
        _tips.RemoveAll();
        _fieldTable.RowStyles.Clear();
        _fieldTable.RowCount = _game.Fields.Count;

        // Sizes created at runtime aren't auto-scaled, so convert from 96-DPI units.
        int editorW = LogicalToDeviceUnits(130);
        int gap = LogicalToDeviceUnits(12);
        var cellMargin = new Padding(0, LogicalToDeviceUnits(4), gap, LogicalToDeviceUnits(4));

        int row = 0;
        foreach (var f in _game.Fields)
        {
            _fieldTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            string range = f.Editable ? $"Range 0 – {f.Max:N0}" : "Shown for reference; not editable.";

            var lbl = new Label
            {
                Text = f.Label, AutoSize = true, Anchor = AnchorStyles.Left,
                Margin = cellMargin, ForeColor = f.Editable ? Theme.Gold : Theme.TextMuted,
                BackColor = Color.Transparent, Font = Theme.Body(11f, FontStyle.Bold | FontStyle.Italic),
            };

            // Description takes whatever width is left and ellipsises rather than pushing
            // the editors off the edge on narrow windows.
            var desc = new Label
            {
                Text = f.Description, AutoSize = false, AutoEllipsis = true, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Height = LogicalToDeviceUnits(24),
                Margin = cellMargin, ForeColor = Theme.Silver,
                BackColor = Color.Transparent, Font = Theme.Body(10f),
            };

            Control editor;
            if (f.Editable)
            {
                var num = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = f.Max,
                    Width = editorW,
                    ThousandsSeparator = f.Max > 9999,
                    TextAlign = HorizontalAlignment.Right,
                    Anchor = AnchorStyles.Left,
                    Margin = cellMargin,
                    Tag = f,
                    Font = Theme.Body(11f, FontStyle.Bold),
                };
                StyleNumeric(num);
                num.ValueChanged += (_, _) => OnFieldChanged(f, num);
                editor = num;
            }
            else
            {
                editor = new TextBox
                {
                    ReadOnly = true,
                    TabStop = false,
                    Width = editorW,
                    TextAlign = HorizontalAlignment.Right,
                    Anchor = AnchorStyles.Left,
                    Margin = cellMargin,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Theme.FieldRO,
                    ForeColor = Theme.TextMuted,
                    Tag = f,
                    Font = Theme.Body(11f, FontStyle.Bold),
                };
            }

            var hint = new Label
            {
                Text = f.Editable ? "" : "view only",
                AutoSize = true, Anchor = AnchorStyles.Left,
                Margin = new Padding(0, LogicalToDeviceUnits(4), 0, LogicalToDeviceUnits(4)),
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent, Font = Theme.Body(8.5f, FontStyle.Italic),
            };

            foreach (Control c in new Control[] { lbl, desc, editor })
            {
                _tips.SetToolTip(c, $"{f.Description}\n{range}");
            }

            _fieldEditors[f.Key] = editor;
            _fieldHints[f.Key] = hint;
            _fieldTable.Controls.Add(lbl, 0, row);
            _fieldTable.Controls.Add(desc, 1, row);
            _fieldTable.Controls.Add(editor, 2, row);
            _fieldTable.Controls.Add(hint, 3, row);
            row++;
        }

        _fieldTable.ResumeLayout();
        FitFieldTable();
    }

    private void OnFieldChanged(FieldDef f, NumericUpDown num)
    {
        // Write against the character the editors were populated for, not whatever is
        // selected now, so a value committed during a selection change lands correctly.
        if (_loading || _save is null || _bound is not { } c || !c.Offsets.TryGetValue(f.Key, out int off))
        {
            return;
        }

        if (_save.Write(off, f.Width, (long)num.Value))
        {
            MarkEdited(c.Name);
            _lstChars.Invalidate();      // refresh LV / HP / MP on the card
            _namePlate.Invalidate();
        }
    }

    // ---- Actions ------------------------------------------------------------------

    private void OpenFile()
    {
        if (!ConfirmDiscardOrSave("opening another file"))
        {
            return;
        }

        using var dlg = new OpenFileDialog
        {
            Title = $"Open a {_game.Title} save state",
            Filter = "Kega Fusion save states (*.gs*)|*.gs*|All files (*.*)|*.*",
        };

        if (_save is not null)
        {
            dlg.InitialDirectory = Path.GetDirectoryName(_save.Path);
        }

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            LoadPath(dlg.FileName, confirmed: true);
        }
    }

    private void LoadPath(string path, bool confirmed = false)
    {
        if (!confirmed && !ConfirmDiscardOrSave("opening another file"))
        {
            return;
        }

        try
        {
            var save = SaveState.Open(path, _game);
            _game.Prepare(save);
            _save = save;
            _edited.Clear();
            PopulateCharacters();
            UpdateState();
        }
        catch (InvalidDataException ex)
        {
            MessageBox.Show(this, ex.Message, "Not a matching save state", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            Log.Write($"{ex.Message} Occurred while opening '{path}'.");
            MessageBox.Show(this, $"Could not open the save state:\n\n{ex.Message}", "Open failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReloadFromDisk()
    {
        if (_save is null)
        {
            return;
        }

        if (Dirty && MessageBox.Show(this, "Discard all unsaved changes and reload the file from disk?",
                "Reload", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        string path = _save.Path;
        string? keep = Current?.Name;
        _edited.Clear();
        LoadPath(path, confirmed: true);
        SelectCharacterByName(keep);
    }

    /// <summary>Writes every pending edit — for all characters and gold — in one save.</summary>
    private bool SaveFile()
    {
        if (_save is null)
        {
            return false;
        }

        // Commit a value still being typed in a field (e.g. when saving with Ctrl+S).
        Validate();

        if (!Dirty)
        {
            MessageBox.Show(this, "There are no changes to save.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        if (_save.ChangedOnDisk() && MessageBox.Show(this,
                "The save state on disk has changed since it was opened (the emulator may have written to this slot).\n\n" +
                "Overwrite it with your edits?",
                "File changed on disk", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return false;
        }

        try
        {
            bool firstSave = _save.BackupPath is null;
            int count = _edited.Count(n => n != GoldKey);
            bool gold = _edited.Contains(GoldKey);
            _save.Save();
            _edited.Clear();
            UpdateState();

            string what = count switch
            {
                0 => "gold",
                1 => gold ? "1 character and gold" : "1 character",
                _ => gold ? $"{count} characters and gold" : $"{count} characters",
            };
            string bak = firstSave && _save.BackupPath is { } b
                ? $"\n\nThe original was backed up to:\n{b}"
                : "";
            MessageBox.Show(this, $"Saved changes to {what}.{bak}\n\nLoad the state in Kega Fusion to use it.",
                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }
        catch (Exception ex)
        {
            Log.Write($"{ex.Message} Occurred while saving the save state to disk.");
            MessageBox.Show(this, $"The save state could not be written:\n\n{ex.Message}", "Save failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    /// <summary>Returns false if the user cancels. Offers to save pending edits first.</summary>
    private bool ConfirmDiscardOrSave(string action)
    {
        Validate();
        if (_save is null || !Dirty)
        {
            return true;
        }

        var answer = MessageBox.Show(this,
            $"You have unsaved changes ({DescribeEdited()}).\n\nSave them before {action}?",
            "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

        return answer switch
        {
            DialogResult.Yes => SaveFile(),
            DialogResult.No => true,
            _ => false,
        };
    }

    private void CloseSave()
    {
        _save = null;
        _bound = null;
        _edited.Clear();
        _chars.Clear();
        _lstChars.Items.Clear();
        _itemList.Items.Clear();
        _spellList.Items.Clear();
        UpdateState();
    }

    private static void ClearErrorLog()
    {
        try
        {
            if (File.Exists(Log.FilePath))
            {
                File.Delete(Log.FilePath);
            }

            MessageBox.Show("The error log has been cleared.", "Error Log", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{ex.Message} Occurred while clearing the error log.", "Error Log",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowInstructions()
    {
        MessageBox.Show(this,
            "How to use\r\n\r\n" +
            "1.  Pick the game (GAME box at the top, or the Game menu).\r\n" +
            "2.  In Kega Fusion 3.64, save a state, then File ▸ Open… and pick it\r\n" +
            "    (or drag the file onto the window).\r\n" +
            "3.  Choose a force member on the left and edit their STATUS.\r\n" +
            "    Gold-labelled rows are editable; grey rows are shown for reference.\r\n" +
            "    ITEM and MAGIC show what each character carries (view only).\r\n" +
            "4.  Edit as many characters as you like — every change is kept, and\r\n" +
            "    edited characters are marked ● on the list.\r\n" +
            "5.  Click SAVE (Ctrl+S) to write all changes at once. The first save\r\n" +
            "    copies the original to <file>.bak next to it.\r\n" +
            "6.  Load the state in Kega Fusion.\r\n\r\n" +
            "File ▸ Reload from Disk throws away unsaved edits.",
            "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ---- Population / state -------------------------------------------------------

    private void PopulateCharacters()
    {
        _bound = null;
        _lstChars.BeginUpdate();
        _lstChars.Items.Clear();
        _chars.Clear();
        if (_save is not null)
        {
            foreach (var c in _game.BuildCharacters(_save))
            {
                _chars.Add(c);
                _lstChars.Items.Add(c.Name);
            }
        }

        _lstChars.EndUpdate();
        if (_lstChars.Items.Count > 0)
        {
            _lstChars.SelectedIndex = 0;
        }
        else
        {
            LoadSelectedCharacter();
        }
    }

    private void SelectCharacterByName(string? name)
    {
        if (name is null)
        {
            return;
        }

        int idx = _chars.FindIndex(c => c.Name == name);
        if (idx >= 0)
        {
            _lstChars.SelectedIndex = idx;
        }
    }

    private void LoadSelectedCharacter()
    {
        var c = Current;
        _bound = c;
        _loading = true;
        foreach (var (key, editor) in _fieldEditors)
        {
            long? v = _save is not null && c is not null ? _game.Get(_save, c, key) : null;
            switch (editor)
            {
                case NumericUpDown num:
                    // Fields a character doesn't have (e.g. Hiro's MP) are hidden and explained.
                    bool missing = v is null && c is not null;
                    num.Visible = !missing;
                    num.Value = v is long n ? Math.Clamp(n, (long)num.Minimum, (long)num.Maximum) : 0;
                    if (_fieldHints.TryGetValue(key, out var hint))
                    {
                        hint.Text = missing ? $"— not used by {c!.Name}" : "";
                    }

                    break;
                case TextBox tb:
                    tb.Text = v is long t ? t.ToString("N0") : "—";
                    break;
            }
        }

        _loading = false;
        _namePlate.Invalidate();
        RenderSlots(c);
    }

    private void RenderSlots(CharacterDef? c)
    {
        _itemList.BeginUpdate();
        _spellList.BeginUpdate();
        _itemList.Items.Clear();
        _spellList.Items.Clear();

        if (_save is not null && c is not null && _game.HasInventory)
        {
            var items = _game.Items(_save, c);
            foreach (var it in items)
            {
                _itemList.Items.Add(it);
            }

            if (items.Count == 0)
            {
                _itemList.Items.Add("— no items —");
            }

            var spells = _game.Spells(_save, c);
            foreach (var sp in spells)
            {
                _spellList.Items.Add(sp);
            }

            if (spells.Count == 0)
            {
                _spellList.Items.Add($"{c.Name} has no spells.");
            }

            int carried = items.Count(i => !i.Empty);
            _itemsNote.Text = $"{carried} item(s) carried — view only." +
                              (items.Any(i => i.Equipped) ? "  Gold = equipped." : "");
            int known = spells.Count(s => !s.Empty);
            _magicNote.Text = $"{known} spell(s) learned — view only." +
                              (_game.MagicNote is { } note ? "  " + note : "");
        }
        else
        {
            _itemsNote.Text = _magicNote.Text = "";
        }

        _itemList.EndUpdate();
        _spellList.EndUpdate();
    }

    private void UpdateState()
    {
        bool loaded = _save is not null;
        _mnuSave.Enabled = _btnSave.Enabled = loaded;
        _mnuReload.Enabled = loaded;
        _numGold.Enabled = loaded;
        _tabs.Enabled = loaded;

        _loading = true;
        if (loaded)
        {
            long gold = _save!.Read(_game.GoldOffset, _game.GoldWidth);
            _numGold.Value = Math.Clamp(gold, (long)_numGold.Minimum, (long)_numGold.Maximum);
        }
        else
        {
            _numGold.Value = 0;
        }

        _loading = false;

        if (!loaded)
        {
            _lblFile.Text = $"No save state loaded.  File ▸ Open… a {_game.Title} state, or drag one here.";
            _lblInfo.Text = "";
            LoadSelectedCharacter();
        }
        else
        {
            _lblFile.Text = _save!.Path;
            string? info = _game.InfoLine(_save);
            _lblInfo.Text = $"{_game.Title}   •   {_chars.Count} characters" + (info is null ? "" : $"   •   {info}");
        }

        UpdateBanner();
        UpdateTitle();
        _lstChars.Invalidate();
        _namePlate.Invalidate();
    }

    private void MarkEdited(string key)
    {
        if (_edited.Add(key))
        {
            UpdateBanner();
            UpdateTitle();
            _lstChars.Invalidate();
        }
    }

    private string DescribeEdited()
    {
        var names = _edited.Where(n => n != GoldKey).ToList();
        var parts = new List<string>();
        if (names.Count > 0)
        {
            parts.Add(names.Count <= 4 ? string.Join(", ", names) : $"{string.Join(", ", names.Take(4))} +{names.Count - 4} more");
        }

        if (_edited.Contains(GoldKey))
        {
            parts.Add("gold");
        }

        return string.Join(" and ", parts);
    }

    private void UpdateBanner()
    {
        if (Dirty)
        {
            _lblBanner.Visible = true;
            _lblBanner.Height = 30;
            _lblBanner.BackColor = Color.FromArgb(0x3A, 0x2A, 0x08);
            _lblBanner.ForeColor = Theme.Gold;
            _lblBanner.Text = $"●  Unsaved changes: {DescribeEdited()}.   Click SAVE to write them all" +
                              (_save?.BackupPath is null ? " (the original is backed up to .bak first)." : ".");
        }
        else
        {
            _lblBanner.Visible = false;
            _lblBanner.Height = 0;
        }
    }

    private void UpdateTitle()
    {
        const string baseTitle = "Shining Editor";
        Text = _save is null
            ? $"{baseTitle} — {_game.Title}"
            : $"{baseTitle} — {_game.Title} — {Path.GetFileName(_save.Path)}{(Dirty ? " *" : "")}";
    }
}
