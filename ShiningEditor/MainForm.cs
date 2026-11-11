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

        // One known good class-table start. If you later confirm a different offset for your format/version,
        // just change this constant.
        private const int SfcdClassTableOffset = 0x0D76A0;        
        
        private AppPanel activePanel;
        private List<ShiningForceItem> shiningForceItemsList;
        private List<ShiningForceMagicItem> shiningForceMagicList;
        private List<ShiningForce2Item> shiningForce2ItemsList;
        private List<ShiningForce2MagicItem> shiningForce2MagicList;
        private ShiningForceCDBook _shiningForceCDBook;
        private Dictionary<byte, string> _shiningForceCDItemNamesByRawId;
        private List<ShiningForceCDMagicItem> shiningForceCDMagicList;

        // Cache is per-loaded file path (so changing files reloads table).
        private string? _sfcdClassTableLoadedFromPath;
        private List<string>? _sfcdClassTable;

        private readonly Dictionary<ShiningForceCDBook, Dictionary<byte, string>> _itemNamesByBook = new();
        #endregion

        #region - Class Properties -
        public bool FileLoaded
        {
            get { return fileLoaded; }
            set { fileLoaded = value; }
        }
        public AppPanel ActivePanel
        {
            get { return activePanel; }
            set { activePanel = value; }
        }
        #endregion

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Size the window here, after WinForms has applied DPI auto-scaling.
        FitToScreen();
        CenterToScreen();
    }

        #region - Event Handlers -
        private void MainForm_Load(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.None;
            ShowPanel(AppPanel.All, false);
            FileLoaded = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void shiningInTheDarknessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningInTheDarkness;
            saveStateFileTb.Text = string.Empty;
            ResetShiningControls(true);
            PopulateShiningCharacterList();
            ShowPanel(AppPanel.ShiningInTheDarkness, true);
        }

        private void shiningForceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningForce;
            saveStateFileTb.Text = string.Empty;
            ResetShiningForceControls(true);
            PopulateShiningForceCharacterList();
            ShowPanel(AppPanel.ShiningForce, true);
        }

        private void shiningForce2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningForce2;
            saveStateFileTb.Text = string.Empty;
            ResetShiningForce2Controls(true);
            PopulateShiningForce2CharacterList();
            ShowPanel(AppPanel.ShiningForce2, true);
        }

        private void shiningForceCDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningForceCD;
            saveStateFileTb.Text = string.Empty;
            ResetShiningForceCDControls(true, true);            
            ShowPanel(AppPanel.ShiningForceCD, true);
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

                // Show the open file dialog and capture the selected file
                if (openFD.ShowDialog() != DialogResult.Cancel)
                {
                    saveStateFileTb.Text = openFD.FileName;
                    FileLoaded = true;
                    switch (ActivePanel)
                    {
                        case AppPanel.ShiningInTheDarkness:
                            PopulateShiningCurrentGold();
                            break;

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

        private void shiningCharacterCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (shiningCharacterCmb.SelectedIndex >= 0)
            {
                if (FileLoaded)
                {
                    ResetShiningControls(false);
                    PopulateShiningCharacterDetails(shiningCharacterCmb.SelectedItem as ShiningCharacterItem);
                }
                else
                {
                    MessageBox.Show("You must load a save state file before you can view character data.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

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

        private void ResetShiningControls(bool resetCharacterList)
        {
            if (resetCharacterList)
            {
                shiningCharacterCmb.SelectedIndex = -1;
            }

            shiningCurGoldTb.Text = string.Empty;
            shiningNewGoldTb.Text = string.Empty;
            shiningLevelTb.Text = string.Empty;
            shiningExpTb.Text = string.Empty;
            shiningNewExpTb.Text = string.Empty;
            shiningCurHPTb.Text = string.Empty;
            shiningNewCurHPTb.Text = string.Empty;
            shiningMaxHPTb.Text = string.Empty;
            shiningNewMaxHPTb.Text = string.Empty;
            shiningCurMPTb.Text = string.Empty;
            shiningNewCurMPTb.Text = string.Empty;
            shiningMaxMPTb.Text = string.Empty;
            shiningNewMaxMPTb.Text = string.Empty;
            shiningIQTb.Text = string.Empty;
            shiningNewIQTb.Text = string.Empty;
            shiningSpeedTb.Text = string.Empty;
            shiningLuckTb.Text = string.Empty;
            shiningNewLuckTb.Text = string.Empty;
            shiningAttackTb.Text = string.Empty;
            shiningNewAttackTb.Text = string.Empty;
            shiningDefTb.Text = string.Empty;
        }

        private void PopulateShiningCharacterList()
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

            shiningForceNewGoldTb.Text = "";
            shiningForceLevelTb.Text = "";
            shiningForceAttackTb.Text = "";
            shiningForceNewAttackTb.Text = "";
            shiningForceDefenseTb.Text = "";
            shiningForceNewDefenseTb.Text = "";
            shiningForceAgilityTb.Text = "";
            shiningForceNewAgilityTb.Text = "";
            shiningForceMoveTb.Text = "";
            shiningForceNewMoveTb.Text = "";
            shiningForceCurExpTb.Text = "";
            shiningForceNewExpTb.Text = "";
            shiningForceCurHPTb.Text = "";
            shiningForceNewCurHPTb.Text = "";
            shiningForceMaxHPTb.Text = "";
            shiningForceNewMaxHPTb.Text = "";
            shiningForceCurMPTb.Text = "";
            shiningForceNewCurMPTb.Text = "";
            shiningForceMaxMPTb.Text = "";
            shiningForceNewMaxMPTb.Text = "";
            shiningForceItemsLB.Items.Clear();
            shiningForceMagicLB.Items.Clear();
        }

        private void PopulateShiningForceCharacterList()
        {
            shiningForceCharacterCmb.Items.Clear();

            ShiningForceCharacterItem heroItem = new ShiningForceCharacterItem(
                "Hero",
                "C115",
                "C116",
                "C117",
                "C118",
                "C119",
                "C11B",
                "C11D",
                "C11F",
                "C120",
                "C121",
                new string[] {"C124", "C125", "C126", "C127"},
                new string[] {"C128", "C129", "C12A", "C12B"}
            );
            shiningForceCharacterCmb.Items.Add(heroItem);

            ShiningForceCharacterItem maeItem = new ShiningForceCharacterItem(
                "Mae",
                "C13D",
                "C13E",
                "C13F",
                "C140",
                "C141",
                "C143",
                "C145",
                "C147",
                "C148",
                "C149",
                new string[] { "C14C", "C14D", "C14E", "C14F" },
                new string[] { "C150", "C151", "C152", "C153" }
            );
            shiningForceCharacterCmb.Items.Add(maeItem);

            ShiningForceCharacterItem pelleItem = new ShiningForceCharacterItem(
                "Pelle",
                "C165",
                "C166",
                "C167",
                "C168",
                "C169",
                "C16B",
                "C16D",
                "C16F",
                "C170",
                "C171",
                new string[] { "C174", "C175", "C176", "C177" },
                new string[] { "C178", "C179", "C17A", "C17B" }
            );
            shiningForceCharacterCmb.Items.Add(pelleItem);

            ShiningForceCharacterItem kenItem = new ShiningForceCharacterItem(
                "Ken",
                "C18D",
                "C18E",
                "C18F",
                "C190",
                "C191",
                "C193",
                "C195",
                "C197",
                "C198",
                "C199",
                new string[] { "C19C", "C19D", "C19E", "C19F" },
                new string[] { "C1A0", "C1A1", "C1A2", "C1A3" }
            );
            shiningForceCharacterCmb.Items.Add(kenItem);

            ShiningForceCharacterItem vankarItem = new ShiningForceCharacterItem(
                "Vankar",
                "C1B5",
                "C1B6",
                "C1B7",
                "C1B8",
                "C1B9",
                "C1BB",
                "C1BD",
                "C1BF",
                "C1C0",
                "C1C1",
                new string[] { "C1C4", "C1C5", "C1C6", "C1C7" },
                new string[] { "C1C8", "C1C9", "C1CA", "C1CB" }
            );
            shiningForceCharacterCmb.Items.Add(vankarItem);

            ShiningForceCharacterItem earnestItem = new ShiningForceCharacterItem(
                "Earnest",
                "C1DD",
                "C1DE",
                "C1DF",
                "C1E0",
                "C1E1",
                "C1E3",
                "C1E5",
                "C1E7",
                "C1E8",
                "C1E9",
                new string[] { "C1EC", "C1ED", "C1EE", "C1EF" },
                new string[] { "C1F0", "C1F1", "C1F2", "C1F3" }
            );
            shiningForceCharacterCmb.Items.Add(earnestItem);

            ShiningForceCharacterItem arthurItem = new ShiningForceCharacterItem(
                "Aurthur",
                "C205",
                "C206",
                "C207",
                "C208",
                "C209",
                "C20B",
                "C20D",
                "C20F",
                "C210",
                "C211",
                new string[] { "C214", "C215", "C216", "C217" },
                new string[] { "C218", "C219", "C21A", "C21B" }
            );
            shiningForceCharacterCmb.Items.Add(arthurItem);

            ShiningForceCharacterItem gortItem = new ShiningForceCharacterItem(
                "Gort",
                "C22D",
                "C22E",
                "C22F",
                "C230",
                "C231",
                "C233",
                "C235",
                "C237",
                "C238",
                "C239",
                new string[] { "C23C", "C23D", "C23E", "C23F" },
                new string[] { "C240", "C241", "C242", "C243" }
            );
            shiningForceCharacterCmb.Items.Add(gortItem);

            ShiningForceCharacterItem lukeItem = new ShiningForceCharacterItem(
                "Luke",
                "C255",
                "C256",
                "C257",
                "C258",
                "C259",
                "C25B",
                "C25D",
                "C25F",
                "C260",
                "C261",
                new string[] { "C264", "C265", "C266", "C267" },
                new string[] { "C268", "C269", "C26A", "C26B" }
            );
            shiningForceCharacterCmb.Items.Add(lukeItem);

            ShiningForceCharacterItem guntzItem = new ShiningForceCharacterItem(
                "Guntz",
                "C27D",
                "C27E",
                "C27F",
                "C280",
                "C281",
                "C283",
                "C285",
                "C287",
                "C288",
                "C289",
                new string[] { "C28C", "C28D", "C28E", "C28F" },
                new string[] { "C290", "C291", "C292", "C293" }
            );
            shiningForceCharacterCmb.Items.Add(guntzItem);

            ShiningForceCharacterItem anriItem = new ShiningForceCharacterItem(
                "Anri",
                "C2A5",
                "C2A6",
                "C2A7",
                "C2A8",
                "C2A9",
                "C2AB",
                "C2AD",
                "C2AF",
                "C2B0",
                "C2B1",
                new string[] { "C2B4", "C2B5", "C2B6", "C2B7" },
                new string[] { "C2B8", "C2B9", "C2BA", "C2BB" }
            );
            shiningForceCharacterCmb.Items.Add(anriItem);

            ShiningForceCharacterItem alefItem = new ShiningForceCharacterItem(
                "Alef",
                "C2CD",
                "C2CE",
                "C2CF",
                "C2D0",
                "C2D1",
                "C2D3",
                "C2D5",
                "C2D7",
                "C2D8",
                "C2D9",
                new string[] { "C2DC", "C2DD", "C2DE", "C2DF" },
                new string[] { "C2E0", "C2E1", "C2E2", "C2E3" }
            );
            shiningForceCharacterCmb.Items.Add(alefItem);

            ShiningForceCharacterItem taoItem = new ShiningForceCharacterItem(
                "Tao",
                "C2F5",
                "C2F6",
                "C2F7",
                "C2F8",
                "C2F9",
                "C2FB",
                "C2FD",
                "C2FF",
                "C300",
                "C301",
                new string[] { "C304", "C305", "C306", "C307" },
                new string[] { "C308", "C309", "C30A", "C30B" }
            );
            shiningForceCharacterCmb.Items.Add(taoItem);

            ShiningForceCharacterItem domingoItem = new ShiningForceCharacterItem(
                "Domingo",
                "C31D",
                "C31E",
                "C31F",
                "C320",
                "C321",
                "C323",
                "C325",
                "C327",
                "C328",
                "C329",
                new string[] { "C32C", "C32D", "C32E", "C32F" },
                new string[] { "C330", "C331", "C332", "C333" }
            );
            shiningForceCharacterCmb.Items.Add(domingoItem);

            ShiningForceCharacterItem loweItem = new ShiningForceCharacterItem(
                "Lowe",
                "C345",
                "C346",
                "C347",
                "C348",
                "C349",
                "C34B",
                "C34D",
                "C34F",
                "C350",
                "C351",
                new string[] { "C354", "C355", "C356", "C357" },
                new string[] { "C358", "C359", "C35A", "C35B" }
            );
            shiningForceCharacterCmb.Items.Add(loweItem);

            ShiningForceCharacterItem khrisItem = new ShiningForceCharacterItem(
                "Khris",
                "C36D",
                "C36E",
                "C36F",
                "C370",
                "C371",
                "C373",
                "C375",
                "C377",
                "C378",
                "C379",
                new string[] { "C37C", "C37D", "C37E", "C37F" },
                new string[] { "C380", "C381", "C382", "C383" }
            );
            shiningForceCharacterCmb.Items.Add(khrisItem);

            ShiningForceCharacterItem torasuItem = new ShiningForceCharacterItem(
                "Torasu",
                "C395",
                "C396",
                "C397",
                "C398",
                "C399",
                "C39B",
                "C39D",
                "C39F",
                "C3A0",
                "C3A1",
                new string[] { "C3A4", "C3A5", "C3A6", "C3A7" },
                new string[] { "C3A8", "C3A9", "C3AA", "C3AB" }
            );
            shiningForceCharacterCmb.Items.Add(torasuItem);

            ShiningForceCharacterItem gongItem = new ShiningForceCharacterItem(
                "Gong",
                "C3BD",
                "C3BE",
                "C3BF",
                "C3C0",
                "C3C1",
                "C3C3",
                "C3C5",
                "C3C7",
                "C3C8",
                "C3C9",
                new string[] { "C3CC", "C3CD", "C3CE", "C3CF" },
                new string[] { "C3D0", "C3D1", "C3D2", "C3D3" }
            );
            shiningForceCharacterCmb.Items.Add(gongItem);

            ShiningForceCharacterItem dianeItem = new ShiningForceCharacterItem(
                "Diane",
                "C3E5",
                "C3E6",
                "C3E7",
                "C3E8",
                "C3E9",
                "C3EB",
                "C3ED",
                "C3EF",
                "C3F0",
                "C3F1",
                new string[] { "C3F4", "C3F5", "C3F6", "C3F7" },
                new string[] { "C3F8", "C3F9", "C3FA", "C3FB" }
            );
            shiningForceCharacterCmb.Items.Add(dianeItem);

            ShiningForceCharacterItem hansItem = new ShiningForceCharacterItem(
                "Hans",
                "C40D",
                "C40E",
                "C40F",
                "C410",
                "C411",
                "C413",
                "C415",
                "C417",
                "C418",
                "C419",
                new string[] { "C41C", "C41D", "C41E", "C41F" },
                new string[] { "C420", "C421", "C422", "C423" }
            );
            shiningForceCharacterCmb.Items.Add(hansItem);

            ShiningForceCharacterItem lyleItem = new ShiningForceCharacterItem(
                "Lyle",
                "C435",
                "C436",
                "C437",
                "C438",
                "C439",
                "C43B",
                "C43D",
                "C43F",
                "C440",
                "C441",
                new string[] { "C444", "C445", "C446", "C447" },
                new string[] { "C448", "C449", "C44A", "C44B" }
            );
            shiningForceCharacterCmb.Items.Add(lyleItem);

            ShiningForceCharacterItem amonItem = new ShiningForceCharacterItem(
                "Amon",
                "C45D",
                "C45E",
                "C45F",
                "C460",
                "C461",
                "C463",
                "C465",
                "C467",
                "C468",
                "C469",
                new string[] { "C46C", "C46D", "C46E", "C46F" },
                new string[] { "C470", "C471", "C472", "C473" }
            );
            shiningForceCharacterCmb.Items.Add(amonItem);

            ShiningForceCharacterItem balbaroyItem = new ShiningForceCharacterItem(
                "Balbaroy",
                "C485",
                "C486",
                "C487",
                "C488",
                "C489",
                "C48B",
                "C48D",
                "C48F",
                "C490",
                "C491",
                new string[] { "C494", "C495", "C496", "C497" },
                new string[] { "C498", "C499", "C49A", "C49B" }
            );
            shiningForceCharacterCmb.Items.Add(balbaroyItem);

            ShiningForceCharacterItem kokichiItem = new ShiningForceCharacterItem(
                "Kokichi",
                "C4AD",
                "C4AE",
                "C4AF",
                "C4B0",
                "C4B1",
                "C4B3",
                "C4B5",
                "C4B7",
                "C4B8",
                "C4B9",
                new string[] { "C4BC", "C4BD", "C4BE", "C4BF" },
                new string[] { "C4C0", "C4C1", "C4C2", "C4C3" }
            );
            shiningForceCharacterCmb.Items.Add(kokichiItem);

            ShiningForceCharacterItem bleuItem = new ShiningForceCharacterItem(
                "Bleu",
                "C4D5",
                "C4D6",
                "C4D7",
                "C4D8",
                "C4D9",
                "C4DB",
                "C4DD",
                "C4DF",
                "C4E0",
                "C4E1",
                new string[] { "C4E4", "C4E5", "C4E6", "C4E7" },
                new string[] { "C4E8", "C4E9", "C4EA", "C4EB" }
            );
            shiningForceCharacterCmb.Items.Add(bleuItem);

            ShiningForceCharacterItem adamItem = new ShiningForceCharacterItem(
                "Adam",
                "C4FD",
                "C4FE",
                "C4FF",
                "C500",
                "C501",
                "C503",
                "C505",
                "C507",
                "C508",
                "C509",
                new string[] { "C50C", "C50D", "C50E", "C50F" },
                new string[] { "C510", "C511", "C512", "C513" }
            );
            shiningForceCharacterCmb.Items.Add(adamItem);

            ShiningForceCharacterItem zyloItem = new ShiningForceCharacterItem(
                "Zylo",
                "C525",
                "C526",
                "C527",
                "C528",
                "C529",
                "C52B",
                "C52D",
                "C52F",
                "C530",
                "C531",
                new string[] { "C534", "C535", "C536", "C537" },
                new string[] { "C538", "C539", "C53A", "C53B" }
            );
            shiningForceCharacterCmb.Items.Add(zyloItem);

            ShiningForceCharacterItem musashiItem = new ShiningForceCharacterItem(
                "Musashi",
                "C54D",
                "C54E",
                "C54F",
                "C550",
                "C551",
                "C553",
                "C555",
                "C557",
                "C558",
                "C559",
                new string[] { "C55C", "C55D", "C55E", "C55F" },
                new string[] { "C560", "C561", "C562", "C563" }
            );
            shiningForceCharacterCmb.Items.Add(musashiItem);

            ShiningForceCharacterItem hanzouItem = new ShiningForceCharacterItem(
                "Hanzou",
                "C575",
                "C576",
                "C577",
                "C578",
                "C579",
                "C57B",
                "C57D",
                "C57F",
                "C580",
                "C581",
                new string[] { "C584", "C585", "C586", "C587" },
                new string[] { "C588", "C589", "C58A", "C58B" }
            );
            shiningForceCharacterCmb.Items.Add(hanzouItem);

            ShiningForceCharacterItem jogurtItem = new ShiningForceCharacterItem(
                "Jogurt",
                "C59D",
                "C59E",
                "C59F",
                "C5A0",
                "C5A1",
                "C5A3",
                "C5A5",
                "C5A7",
                "C5A8",
                "C5A9",
                new string[] { "C5AC", "C5AD", "C5AE", "C5AF" },
                new string[] { "C5B0", "C5B1", "C5B2", "C5B3" }
            );
            shiningForceCharacterCmb.Items.Add(jogurtItem);

            shiningForceCharacterCmb.DisplayMember = "Name";
        }

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        };
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
            {
                LoadPath(files[0]);
            }
        };
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

        private void PopulateShiningForce2CharacterList()
        {
            shiningForce2CharacterCmb.Items.Clear();

            ShiningForce2CharacterItem heroItem = new ShiningForce2CharacterItem("Bowie (Hero)",
                "10C83",
                "10C8A",
                "10C8B",
                "10C8C",
                "10C8D",
                "10C8E",
                "10C8F",
                "10C90",
                "10C91",
                "10CA8",
                "10C86",
                "10C84",
                "10C89",
                "10C88",
                new string[] { "10C99", "10C98", "10C9D", "10C9F"},
                new string[] { "10CA0", "10CA1", "10CA2", "10CA3"},
                "10CAA", 
                "10CAE");
            shiningForce2CharacterCmb.Items.Add(heroItem);

            ShiningForce2CharacterItem sarahItem = new ShiningForce2CharacterItem("Sarah",
                "10CBB",
                "10CC2",
                "10CC3",
                "10CC4",
                "10CC5",
                "10CC6",
                "10CC7",
                "10CC8",
                "10CC9",
                "10CE0",
                "10CBE",
                "10CBC",
                "10CC1",
                "10CC0",
                new string[] { "10CD1", "10CD3", "10CD5", "10CD7" },
                new string[] { "10CD8", "10CD9", "10CDA", "10CDB" },
                "10CE2",
                "10CE6");
            shiningForce2CharacterCmb.Items.Add(sarahItem);

            ShiningForce2CharacterItem chesterItem = new ShiningForce2CharacterItem("Chester",
                "10CF3",
                "10CFA",
                "10CFB",
                "10CFC",
                "10CFD",
                "10CFE",
                "10CFF",
                "10D00",
                "10D01",
                "10D18",
                "10CF6",
                "10CF4",
                "10CF9",
                "10CF8",
                new string[] { "10D09", "10D0B", "10D0D", "10D0F" },
                new string[] { "10D10", "10D11", "10D12", "10D13" },
                "10D1A",
                "10D1E");
            shiningForce2CharacterCmb.Items.Add(chesterItem);

            ShiningForce2CharacterItem jahaItem = new ShiningForce2CharacterItem("Jaha",
                "10D2B",
                "10D32",
                "10D33",
                "10D34",
                "10D35",
                "10D36",
                "10D37",
                "10D38",
                "10D39",
                "10D50",
                "10D2E",
                "10D2C",
                "10D31",
                "10D30",
                new string[] { "10D41", "10D43", "10D45", "10D47" },
                new string[] { "10D48", "10D49", "10D4A", "10D4B" },
                "10D52",
                "10D56");
            shiningForce2CharacterCmb.Items.Add(jahaItem);

            ShiningForce2CharacterItem kazinItem = new ShiningForce2CharacterItem("Kazin",
                "10D63",
                "10D6A",
                "10D6B",
                "10D6C",
                "10D6D",
                "10D6E",
                "10D6F",
                "10D70",
                "10D71",
                "10D88",
                "10D66",
                "10D64",
                "10D69",
                "10D68",
                new string[] { "10D79", "10D7B", "10D7D", "10D7F" },
                new string[] { "10D80", "10D81", "10D82", "10D83" },
                "10D8A",
                "10D8E");
            shiningForce2CharacterCmb.Items.Add(kazinItem);

            ShiningForce2CharacterItem sladeItem = new ShiningForce2CharacterItem("Slade",
                "10D9B",
                "10DA2",
                "10DA3",
                "10DA4",
                "10DA5",
                "10DA6",
                "10DA7",
                "10DA8",
                "10DA9",
                "10DC0",
                "10D9E",
                "10D9C",
                "10DA1",
                "10DA0",
                new string[] { "10DB1", "10DB3", "10DB5", "10DB7" },
                new string[] { "10DB8", "10DB9", "10DBA", "10DBB" },
                "10DC2",
                "10DC6");
            shiningForce2CharacterCmb.Items.Add(sladeItem);

            ShiningForce2CharacterItem kiwiItem = new ShiningForce2CharacterItem("Kiwi",
                "10DD3",
                "10DDA",
                "10DDB",
                "10DDC",
                "10DDD",
                "10DDE",
                "10DDF",
                "10DE0",
                "10DE1",
                "10DF8",
                "10DD6",
                "10DD4",
                "10DD9",
                "10DD8",
                new string[] { "10DE9", "10DEB", "10DED", "10DEF" },
                new string[] { "10DF0", "10DF1", "10DF2", "10DF3" },
                "10DFA",
                "10DFE");
            shiningForce2CharacterCmb.Items.Add(kiwiItem);

            ShiningForce2CharacterItem peterItem = new ShiningForce2CharacterItem("Peter",
                "10E0B",
                "10E12",
                "10E13",
                "10E14",
                "10E15",
                "10E16",
                "10E17",
                "10E18",
                "10E19",
                "10E30",
                "10E0E",
                "10E0C",
                "10E11",
                "10E10",
                new string[] { "10E21", "10E23", "10E25", "10E27" },
                new string[] { "10E28", "10E29", "10E2A", "10E2B" },
                "10E32",
                "10E36");
            shiningForce2CharacterCmb.Items.Add(peterItem);

            ShiningForce2CharacterItem mayItem = new ShiningForce2CharacterItem("May",
                "10E43",
                "10E4A",
                "10E4B",
                "10E4C",
                "10E4D",
                "10E4E",
                "10E4F",
                "10E50",
                "10E51",
                "10E68",
                "10E46",
                "10E44",
                "10E49",
                "10E48",
                new string[] { "10E59", "10E5B", "10E5D", "10E5F" },
                new string[] { "10E60", "10E61", "10E62", "10E63" },
                "10E6A",
                "10E6E");
            shiningForce2CharacterCmb.Items.Add(mayItem);

            ShiningForce2CharacterItem gerhaltItem = new ShiningForce2CharacterItem("Gerhalt",
                "10E7B",
                "10E82",
                "10E83",
                "10E84",
                "10E85",
                "10E86",
                "10E87",
                "10E88",
                "10E89",
                "10EA0",
                "10E7E",
                "10E7C",
                "10E81",
                "10E80",
                new string[] { "10E91", "10E93", "10E95", "10E97" },
                new string[] { "10E98", "10E99", "10E9A", "10E9B" },
                "10EA2",
                "10EA6");
            shiningForce2CharacterCmb.Items.Add(gerhaltItem);

            ShiningForce2CharacterItem lukeItem = new ShiningForce2CharacterItem("Luke",
                "10EB3",
                "10EBA",
                "10EBB",
                "10EBC",
                "10EBD",
                "10EBE",
                "10EBF",
                "10EC0",
                "10EC1",
                "10ED8",
                "10EB6",
                "10EB4",
                "10EB9",
                "10EB8",
                new string[] { "10EC9", "10ECB", "10ECD", "10ECF" },
                new string[] { "10ED0", "10ED1", "10ED2", "10ED3" },
                "10EDA",
                "10EDE");
            shiningForce2CharacterCmb.Items.Add(lukeItem);

            ShiningForce2CharacterItem rohdeItem = new ShiningForce2CharacterItem("Rohde",
                "10EEB",
                "10EF2",
                "10EF3",
                "10EF4",
                "10EF5",
                "10EF6",
                "10EF7",
                "10EF8",
                "10EF9",
                "10F10",
                "10EEE",
                "10EEC",
                "10EF1",
                "10EF0",
                new string[] { "10F01", "10F03", "10F05", "10F07" },
                new string[] { "10F08", "10F09", "10F0A", "10F0B" },
                "10F12",
                "10F16");
            shiningForce2CharacterCmb.Items.Add(rohdeItem);

            ShiningForce2CharacterItem rickItem = new ShiningForce2CharacterItem("Rick",
                "10F23",
                "10F2A",
                "10F2B",
                "10F2C",
                "10F2D",
                "10F2E",
                "10F2F",
                "10F30",
                "10F31",
                "10F48",
                "10F26",
                "10F24",
                "10F29",
                "10F28",
                new string[] { "10F39", "10F3B", "10F3D", "10F3F" },
                new string[] { "10F40", "10F41", "10F42", "10F43" },
                "10F4A",
                "10F4E");
            shiningForce2CharacterCmb.Items.Add(rickItem);

            ShiningForce2CharacterItem elricItem = new ShiningForce2CharacterItem("Elric",
                "10F5B",
                "10F62",
                "10F63",
                "10F64",
                "10F65",
                "10F66",
                "10F67",
                "10F68",
                "10F69",
                "10F80",
                "10F5E",
                "10F5C",
                "10F61",
                "10F60",
                new string[] { "10F71", "10F73", "10F75", "10F77" },
                new string[] { "10F78", "10F79", "10F7A", "10F7B" },
                "10F82",
                "10F86");
            shiningForce2CharacterCmb.Items.Add(elricItem);

            ShiningForce2CharacterItem ericItem = new ShiningForce2CharacterItem("Eric",
                "10F93",
                "10F9A",
                "10F9B",
                "10F9C",
                "10F9D",
                "10F9E",
                "10F9F",
                "10FA0",
                "10FA1",
                "10FB8",
                "10F96",
                "10F94",
                "10F99",
                "10F98",
                new string[] { "10FA9", "10FAB", "10FAD", "10FAF" },
                new string[] { "10FB0", "10FB1", "10FB2", "10FB3" },
                "10FBA",
                "10FBE");
            shiningForce2CharacterCmb.Items.Add(ericItem);

            ShiningForce2CharacterItem karnaItem = new ShiningForce2CharacterItem("Karna",
                "10FCB",
                "10FD2",
                "10FD3",
                "10FD4",
                "10FD5",
                "10FD6",
                "10FD7",
                "10FD8",
                "10FD9",
                "10FF0",
                "10FCE",
                "10FCC",
                "10FD1",
                "10FD0",
                new string[] { "10FE1", "10FE3", "10FE5", "10FE7" },
                new string[] { "10FE8", "10FE9", "10FEA", "10FEB" },
                "10FF2",
                "10FF6");
            shiningForce2CharacterCmb.Items.Add(karnaItem);

            ShiningForce2CharacterItem randolfItem = new ShiningForce2CharacterItem("Randolf",
                "11003",
                "1100A",
                "1100B",
                "1100C",
                "1100D",
                "1100E",
                "1100F",
                "11010",
                "11011",
                "11028",
                "11006",
                "11004",
                "11009",
                "11008",
                new string[] { "11019", "1101B", "1101D", "1101F" },
                new string[] { "11020", "11021", "11022", "11023" },
                "1102A",
                "1102E");
            shiningForce2CharacterCmb.Items.Add(randolfItem);

            ShiningForce2CharacterItem tyrinItem = new ShiningForce2CharacterItem("Tyrin",
                "1103B",
                "11042",
                "11043",
                "11044",
                "11045",
                "11046",
                "11047",
                "11048",
                "11049",
                "11060",
                "1103E",
                "1103C",
                "11041",
                "11040",
                new string[] { "11051", "11053", "11055", "11057" },
                new string[] { "11058", "11059", "1105A", "1105B" },
                "11062",
                "11066");
            shiningForce2CharacterCmb.Items.Add(tyrinItem);

            ShiningForce2CharacterItem janetItem = new ShiningForce2CharacterItem("Janet",
                "11073",
                "1107A",
                "1107B",
                "1107C",
                "1107D",
                "1107E",
                "1107F",
                "11080",
                "11081",
                "11098",
                "11076",
                "11074",
                "11079",
                "11078",
                new string[] { "11089", "1108B", "1108D", "1108F" },
                new string[] { "11090", "11091", "11092", "11093" },
                "1109A",
                "1109E");
            shiningForce2CharacterCmb.Items.Add(janetItem);

            ShiningForce2CharacterItem higinsItem = new ShiningForce2CharacterItem("Higins",
                "110AB",
                "110B2",
                "110B3",
                "110B4",
                "110B5",
                "110B6",
                "110B7",
                "110B8",
                "110B9",
                "110D0",
                "110AE",
                "110AC",
                "110B1",
                "110B0",
                new string[] { "110C1", "110C3", "110C5", "110C7" },
                new string[] { "110C8", "110C9", "110CA", "110CB" },
                "110D2",
                "110D6");
            shiningForce2CharacterCmb.Items.Add(higinsItem);

            shiningForce2CharacterCmb.DisplayMember = "Name";
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

        private void LogError(string errMsg)
        {
            // Create a write and open the file
            string filePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            filePath += @"\errorlog.txt";
            TextWriter writer = new StreamWriter(filePath, true);

            // Write the error message to the error log
            writer.WriteLine(errMsg + " Added: " + DateTime.Now.ToString());

            // Close the stream
            writer.Close();
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

        private string GetValueByOffset(string offset, int bytesToRead)
        {
            string value = string.Empty;
            BinaryReader reader = null;

            try
            {
                reader = new BinaryReader(new FileStream(saveStateFileTb.Text, FileMode.Open));
                // Set the position of the reader by the offset
                reader.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                // Read the offset
                value = BitConverter.ToString(reader.ReadBytes(bytesToRead)).Replace("-", null);
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred when attempting to read a value by its offset.");
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred when attempting to read a value by its offset.");
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred when attempting to read a value by its offset.");
            }
            finally
            {
                reader.Close();
                reader.Dispose();
            }

            return value;
        }

        private byte[] GetBytesByOffset(string offset, int bytesToRead)
        {
            using var reader = new BinaryReader(
                new FileStream(saveStateFileTb.Text, FileMode.Open, FileAccess.Read)
            );

            reader.BaseStream.Position =
                long.Parse(offset, System.Globalization.NumberStyles.HexNumber);

            return reader.ReadBytes(bytesToRead);
        }

        private byte[] GetBytesByOffset(int offset, int length)
        {
            using var fs = new FileStream(saveStateFileTb.Text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            fs.Position = offset;

            byte[] buffer = new byte[length];
            int read = fs.Read(buffer, 0, length);


            if (read != length)
            {
                Array.Resize(ref buffer, read);
            }

            return buffer;
        }


        private bool SetValueByOffset(string value, string offset)
        {
            bool success = false;
            BinaryWriter writer = null;

            try
            {
                writer = new BinaryWriter(new FileStream(saveStateFileTb.Text, FileMode.Open));
                writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                int valNum = Convert.ToInt32(value);
                byte[] bytes = BitConverter.GetBytes(valNum).Reverse().ToArray();
                writer.Write(bytes);
                success = true;
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            finally
            {
                writer.Close();
                writer.Dispose();
            }

            return success;
        }

        private bool SetValueByOffset(long value, string offset)
        {
            bool success = false;
            BinaryWriter writer = null;

            try
            {
                writer = new BinaryWriter(new FileStream(saveStateFileTb.Text, FileMode.Open));
                writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
                writer.Write(bytes);

                success = true;
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            finally
            {
                writer.Close();
                writer.Dispose();
            }

            return success;
        }

        private bool SetValueByOffset(int value, string offset, int numBytes = 0)
        {
            bool success = false;
            BinaryWriter writer = null;

            try
            {
                writer = new BinaryWriter(new FileStream(saveStateFileTb.Text, FileMode.Open));
                writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
                if (numBytes == 0)
                {
                    writer.Write(bytes);
                }
                else
                {
                    writer.Write(bytes, 1, numBytes);
                }

                success = true;
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            finally
            {
                writer.Close();
                writer.Dispose();
            }

            return success;
        }

        private bool SetValueByOffset(short value, string offset, int index = 0, int numBytes = 0)
        {
            bool success = false;
            BinaryWriter writer = null;

            try
            {
                writer = new BinaryWriter(new FileStream(saveStateFileTb.Text, FileMode.Open));
                writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
                if (numBytes == 0 && index == 0)
                {
                    writer.Write(bytes);
                }
                else
                {
                    writer.Write(bytes, index, numBytes);
                }

                success = true;
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            finally
            {
                writer.Close();
                writer.Dispose();
            }

            return success;
        }

        private bool SetValueByOffset(ushort value, string offset, int index = 0, int numBytes = 0)
        {
            bool success = false;
            BinaryWriter writer = null;

            try
            {
                writer = new BinaryWriter(new FileStream(saveStateFileTb.Text, FileMode.Open));
                writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();

                if (numBytes == 0)
                {
                    writer.Write(bytes);
                }
                else
                {
                    writer.Write(bytes, index, numBytes);
                }

                success = true;
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            finally
            {
                writer.Close();
                writer.Dispose();
            }

            return success;
        }

        private bool SetUInt32BigEndianByOffset(uint value, string offset)
        {
            try
            {
                using (var stream = new FileStream(saveStateFileTb.Text, FileMode.Open, FileAccess.Write, FileShare.Read))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);

                    // Convert to bytes
                    byte[] bytes = BitConverter.GetBytes(value);

                    // Save is big-endian, so reverse on little-endian machines (Windows)
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(bytes);
                    }

                    writer.Write(bytes); // 4 bytes
                }

                return true;
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred while attempting to write gold to the save state file.");
                return false;
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred while attempting to write gold to the save state file.");
                return false;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write gold to the save state file.");
                return false;
            }
        }

        private bool SetByteByOffset(byte value, string offset)
        {
            try
            {
                using (var writer = new BinaryWriter(new FileStream(saveStateFileTb.Text, FileMode.Open, FileAccess.Write, FileShare.Read)))
                {
                    writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                    writer.Write(value);
                    return true;
                }
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write a byte to the save state file.");
                return false;
            }
        }

        private bool SetValueByOffset(byte value, string offset)
        {
            bool success = false;
            BinaryWriter writer = null;

            try
            {
                writer = new BinaryWriter(new FileStream(saveStateFileTb.Text, FileMode.Open));
                writer.BaseStream.Position = long.Parse(offset, System.Globalization.NumberStyles.HexNumber);
                writer.Write(value);

                success = true;
            }
            catch (IOException ioe)
            {
                LogError(ioe.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (ArgumentException aue)
            {
                LogError(aue.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while attempting to write a value to the save state file.");
                success = false;
            }
            finally
            {
                writer.Close();
                writer.Dispose();
            }

            return success;
        }

        private void UpdateShiningSaveState()
        {
            ShiningCharacterItem charItem = shiningCharacterCmb.SelectedItem as ShiningCharacterItem;
            if (shiningNewGoldTb.Text != string.Empty)
            {
                int gold = 0;
                if (int.TryParse(shiningNewGoldTb.Text, out gold))
                {
                    SetValueByOffset(gold, SHINING_GOLD_LOC);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new gold value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewExpTb.Text != string.Empty)
            {
                int exp = 0;
                if (int.TryParse(shiningNewExpTb.Text, out exp))
                {
                    SetValueByOffset(exp, charItem.ExpLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new experience value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewCurHPTb.Text != string.Empty)
            {
                short hp = 0;
                if (short.TryParse(shiningNewCurHPTb.Text, out hp))
                {
                    SetValueByOffset(hp, charItem.CurHPLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new current HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewMaxHPTb.Text != string.Empty)
            {
                short hp = 0;
                if (short.TryParse(shiningNewMaxHPTb.Text, out hp))
                {
                    SetValueByOffset(hp, charItem.MaxHPLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new max HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewCurMPTb.Text != string.Empty)
            {
                short tp = 0;
                if (short.TryParse(shiningNewCurMPTb.Text, out tp))
                {
                    SetValueByOffset(tp, charItem.CurMPLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new current MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewMaxMPTb.Text != string.Empty)
            {
                short tp = 0;
                if (short.TryParse(shiningNewMaxMPTb.Text, out tp))
                {
                    SetValueByOffset(tp, charItem.MaxMPLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new max MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewIQTb.Text != string.Empty)
            {
                short str = 0;
                if (short.TryParse(shiningNewIQTb.Text, out str))
                {
                    SetValueByOffset(str, charItem.IQLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new IQ value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewLuckTb.Text != string.Empty)
            {
                short str = 0;
                if (short.TryParse(shiningNewLuckTb.Text, out str))
                {
                    SetValueByOffset(str, charItem.LuckLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new luck value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (shiningNewAttackTb.Text != string.Empty)
            {
                short str = 0;
                if (short.TryParse(shiningNewAttackTb.Text, out str))
                {
                    SetValueByOffset(str, charItem.AttackLoc);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new attack value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            MessageBox.Show("The save state update process has completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetShiningControls(false);
            PopulateShiningCurrentGold();
            PopulateShiningCharacterDetails(charItem);
        }

    private void ReloadFromDisk()
    {
        if (_save is null)
        {
            return;
        }

        private void UpdateShiningForceSaveState()
        {
            ShiningForceCharacterItem charItem = shiningForceCharacterCmb.SelectedItem as ShiningForceCharacterItem;
            if (shiningForceNewGoldTb.Text != string.Empty)
            {
                int gold = 0;
                if (int.TryParse(shiningForceNewGoldTb.Text, out gold))
                {
                    SetValueByOffset(gold, SHINING_FORCE_GOLD_LOC, 3);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new gold value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (charItem != null)
            {
                if (shiningForceNewAttackTb.Text != string.Empty)
                {
                    short attack = 0;
                    if (short.TryParse(shiningForceNewAttackTb.Text, out attack))
                    {
                        SetValueByOffset(attack, charItem.AttackLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new attack value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewDefenseTb.Text != string.Empty)
                {
                    short defense = 0;
                    if (short.TryParse(shiningForceNewDefenseTb.Text, out defense))
                    {
                        SetValueByOffset(defense, charItem.DefenseLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new defense value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewAgilityTb.Text != string.Empty)
                {
                    short agility = 0;
                    if (short.TryParse(shiningForceNewAgilityTb.Text, out agility))
                    {
                        SetValueByOffset(agility, charItem.AgilityLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new agility value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewMoveTb.Text != string.Empty)
                {
                    short move = 0;
                    if (short.TryParse(shiningForceNewMoveTb.Text, out move))
                    {
                        SetValueByOffset(move, charItem.MoveLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new move value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewExpTb.Text != string.Empty)
                {
                    short exp = 0;
                    if (short.TryParse(shiningForceNewExpTb.Text, out exp))
                    {
                        SetValueByOffset(exp, charItem.ExperienceLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new experience value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewCurHPTb.Text != string.Empty)
                {
                    short hp = 0;
                    if (short.TryParse(shiningForceNewCurHPTb.Text, out hp))
                    {
                        SetValueByOffset(hp, charItem.CurrentHPLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new current HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewMaxHPTb.Text != string.Empty)
                {
                    short maxHP = 0;
                    if (short.TryParse(shiningForceNewMaxHPTb.Text, out maxHP))
                    {
                        SetValueByOffset(maxHP, charItem.MaxHPLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new max HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewCurMPTb.Text != string.Empty)
                {
                    short mp = 0;
                    if (short.TryParse(shiningForceNewCurMPTb.Text, out mp))
                    {
                        SetValueByOffset(mp, charItem.CurrentMPLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new curent MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceNewMaxMPTb.Text != string.Empty)
                {
                    short maxMP = 0;
                    if (short.TryParse(shiningForceNewMaxMPTb.Text, out maxMP))
                    {
                        SetValueByOffset(maxMP, charItem.MaxMPLoc, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new max MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
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

        private void UpdateShiningForce2SaveState()
        {
            ShiningForce2CharacterItem charItem = shiningForce2CharacterCmb.SelectedItem as ShiningForce2CharacterItem;
            if (shiningForce2NewGoldTb.Text != string.Empty)
            {
                ushort gold = 0;
                if (ushort.TryParse(shiningForce2NewGoldTb.Text, out gold))
                {
                    SetValueByOffset(gold, SHINING_FORCE_2_GOLD_LOC, 0, 2);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new gold value that doesn't exceed 65535.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (charItem != null)
            {
                if (shiningForce2NewAttackBaseTb.Text != string.Empty)
                {
                    short attack = 0;
                    if (short.TryParse(shiningForce2NewAttackBaseTb.Text, out attack))
                    {
                        SetValueByOffset(attack, charItem.AttackBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new attack base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewAttackEquipTb.Text != string.Empty)
                {
                    short attack = 0;
                    if (short.TryParse(shiningForce2NewAttackEquipTb.Text, out attack))
                    {
                        SetValueByOffset(attack, charItem.AttackEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new attack equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewDefenseBaseTb.Text != string.Empty)
                {
                    short defense = 0;
                    if (short.TryParse(shiningForce2NewDefenseBaseTb.Text, out defense))
                    {
                        SetValueByOffset(defense, charItem.DefenseBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new defense base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewDefenseEquipTb.Text != string.Empty)
                {
                    short defense = 0;
                    if (short.TryParse(shiningForce2NewDefenseEquipTb.Text, out defense))
                    {
                        SetValueByOffset(defense, charItem.DefenseEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new defense equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewAgilityBaseTb.Text != string.Empty)
                {
                    short agility = 0;
                    if (short.TryParse(shiningForce2NewAgilityBaseTb.Text, out agility))
                    {
                        SetValueByOffset(agility, charItem.AgilityBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new agility base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewAgilityEquipTb.Text != string.Empty)
                {
                    short agility = 0;
                    if (short.TryParse(shiningForce2NewAgilityEquipTb.Text, out agility))
                    {
                        SetValueByOffset(agility, charItem.AgilityEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new agility equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewMoveBaseTb.Text != string.Empty)
                {
                    short move = 0;
                    if (short.TryParse(shiningForce2NewMoveBaseTb.Text, out move))
                    {
                        SetValueByOffset(move, charItem.MoveBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new move base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewMoveEquipTb.Text != string.Empty)
                {
                    short move = 0;
                    if (short.TryParse(shiningForce2NewMoveEquipTb.Text, out move))
                    {
                        SetValueByOffset(move, charItem.MoveEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new move equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewExpTb.Text != string.Empty)
                {
                    short exp = 0;
                    if (short.TryParse(shiningForce2NewExpTb.Text, out exp))
                    {
                        SetValueByOffset(exp, charItem.ExperienceOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new experience value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewPresentHPTb.Text != string.Empty)
                {
                    short hp = 0;
                    if (short.TryParse(shiningForce2NewPresentHPTb.Text, out hp))
                    {
                        SetValueByOffset(hp, charItem.PresentHPOffset, 0, 0);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new present HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewMaxHPTb.Text != string.Empty)
                {
                    short hp = 0;
                    if (short.TryParse(shiningForce2NewMaxHPTb.Text, out hp))
                    {
                        SetValueByOffset(hp, charItem.MaximumHPOffset, 0, 0);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new max HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewPresentMPTb.Text != string.Empty)
                {
                    short mp = 0;
                    if (short.TryParse(shiningForce2NewPresentMPTb.Text, out mp))
                    {
                        SetValueByOffset(mp, charItem.PresentMPOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new present MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForce2NewMaxMPTb.Text != string.Empty)
                {
                    short mp = 0;
                    if (short.TryParse(shiningForce2NewMaxMPTb.Text, out mp))
                    {
                        SetValueByOffset(mp, charItem.MaximumMPOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new pmax MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
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

        private static string Hex(int value) => value.ToString("X");

        private ShiningForceCDCharacterItem MakeChar(string name, int baseOffset)
        {
            return new ShiningForceCDCharacterItem(
                name,
                baseOffset,
                Hex(baseOffset + 0x00), // FaceClassOffset
                Hex(baseOffset + 0x01), // LevelOffset
                Hex(baseOffset + 0x08), // AttackBaseOffset
                Hex(baseOffset + 0x09), // AttackEquipOffset
                Hex(baseOffset + 0x0A), // DefenseBaseOffset
                Hex(baseOffset + 0x0B), // DefenseEquipOffset
                Hex(baseOffset + 0x0C), // AgilityBaseOffset
                Hex(baseOffset + 0x0D), // AgilityEquipOffset
                Hex(baseOffset + 0x0E), // MoveBaseOffset
                Hex(baseOffset + 0x0F), // MoveEquipOffset

                Hex(baseOffset + 0x26), // ExperienceOffset (1 byte)

                Hex(baseOffset + 0x02), // PresentHPOffset (2 bytes)
                Hex(baseOffset + 0x04), // MaximumHPOffset (2 bytes)
                Hex(baseOffset + 0x06), // PresentMPOffset (1 byte)
                Hex(baseOffset + 0x07), // MaximumMPOffset (1 byte)

                // ITEMS: skip the first pair at base+0x14..0x15
                // real 4 inventory slots are the next 4 ID bytes:
                new[]
                {
                    Hex(baseOffset + 0x17), // slot 1 ID
                    Hex(baseOffset + 0x19), // slot 2 ID
                    Hex(baseOffset + 0x1B), // slot 3 ID
                    Hex(baseOffset + 0x1D), // slot 4 ID
                },

                // Magic: 4 bytes starting at base+0x1E (your logs confirm this)
                new[]
                {
                    Hex(baseOffset + 0x1E),
                    Hex(baseOffset + 0x1F),
                    Hex(baseOffset + 0x20),
                    Hex(baseOffset + 0x21),
                }
            );
        }

        private ShiningForceCDMagicItem GetShiningForceCDMagicItem(string id)
        {
            return shiningForceCDMagicList.FirstOrDefault(i => i.ID.ToUpper() == id.ToUpper());
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

            _sfcdClassTableLoadedFromPath = path;

            // Read file once (it's ~1.1MB; totally fine)
            byte[] all = File.ReadAllBytes(path);

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

        private void UpdateShiningForceCDSaveState()
        {
            ShiningForceCDCharacterItem charItem = shiningForceCDSelectCharacterCmb.SelectedItem as ShiningForceCDCharacterItem;

            if (!string.IsNullOrEmpty(shiningForceCDNewGoldTb.Text))
            {
                if (uint.TryParse(shiningForceCDNewGoldTb.Text, out var newGold))
                {
                    SetUInt32BigEndianByOffset(newGold, SHINING_FORCE_CD_GOLD_LOC);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new gold value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (charItem != null)
            {
                if (shiningForceCDNewAttackBaseTb.Text != string.Empty)
                {
                    short attack = 0;
                    if (short.TryParse(shiningForceCDNewAttackBaseTb.Text, out attack))
                    {
                        SetValueByOffset(attack, charItem.AttackBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new attack base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewAttackEquipTb.Text != string.Empty)
                {
                    short attack = 0;
                    if (short.TryParse(shiningForceCDNewAttackEquipTb.Text, out attack))
                    {
                        SetValueByOffset(attack, charItem.AttackEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new attack equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewDefenseBaseTb.Text != string.Empty)
                {
                    short defense = 0;
                    if (short.TryParse(shiningForceCDNewDefenseBaseTb.Text, out defense))
                    {
                        SetValueByOffset(defense, charItem.DefenseBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new defense base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewDefenseEquipTb.Text != string.Empty)
                {
                    short defense = 0;
                    if (short.TryParse(shiningForceCDNewDefenseEquipTb.Text, out defense))
                    {
                        SetValueByOffset(defense, charItem.DefenseEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new defense equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewAgilityBaseTb.Text != string.Empty)
                {
                    short agility = 0;
                    if (short.TryParse(shiningForceCDNewAgilityBaseTb.Text, out agility))
                    {
                        SetValueByOffset(agility, charItem.AgilityBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new agility base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewAgilityEquipTb.Text != string.Empty)
                {
                    short agility = 0;
                    if (short.TryParse(shiningForceCDNewAgilityEquipTb.Text, out agility))
                    {
                        SetValueByOffset(agility, charItem.AgilityEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new agility equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewMoveBaseTb.Text != string.Empty)
                {
                    short move = 0;
                    if (short.TryParse(shiningForceCDNewMoveBaseTb.Text, out move))
                    {
                        SetValueByOffset(move, charItem.MoveBaseOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new move base value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewMoveEquipTb.Text != string.Empty)
                {
                    short move = 0;
                    if (short.TryParse(shiningForceCDNewMoveEquipTb.Text, out move))
                    {
                        SetValueByOffset(move, charItem.MoveEquipOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new move equip value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewExperienceTb.Text != string.Empty)
                {
                    short exp = 0;
                    if (short.TryParse(shiningForceCDNewExperienceTb.Text, out exp))
                    {
                        SetValueByOffset(exp, charItem.ExperienceOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new experience value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewPresentHPTb.Text != string.Empty)
                {
                    short hp = 0;
                    if (short.TryParse(shiningForceCDNewPresentHPTb.Text, out hp))
                    {
                        SetValueByOffset(hp, charItem.PresentHPOffset, 0, 0);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new present HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewMaxHPTb.Text != string.Empty)
                {
                    short hp = 0;
                    if (short.TryParse(shiningForceCDNewMaxHPTb.Text, out hp))
                    {
                        SetValueByOffset(hp, charItem.MaximumHPOffset, 0, 0);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new max HP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewPresentMPTb.Text != string.Empty)
                {
                    short mp = 0;
                    if (short.TryParse(shiningForceCDNewPresentMPTb.Text, out mp))
                    {
                        SetValueByOffset(mp, charItem.PresentMPOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new present MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (shiningForceCDNewMaxMPTb.Text != string.Empty)
                {
                    short mp = 0;
                    if (short.TryParse(shiningForceCDNewMaxMPTb.Text, out mp))
                    {
                        SetValueByOffset(mp, charItem.MaximumMPOffset, 1, 1);
                    }
                    else
                    {
                        MessageBox.Show("You must enter a numeric value for the new max MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            MessageBox.Show("The save state update process has completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetShiningForceCDControls(false);
            PopulateShiningForceCDCurrentGold();

            if (charItem != null)
            {
                PopulateShiningForceCDCharacterDetails(charItem);
            }
        }       
        #endregion

        #region - Sealed Classes -
        private sealed class SfcdCharSlotDef
        {
            public string SlotId { get; } // stable key you invent, e.g. "Leader", "Archer1"
            public string DefaultName { get; } // fallback display name
            public Dictionary<ShiningForceCDBook, int> OffsetByBook { get; } = new();
            public Dictionary<ShiningForceCDBook, string> NameByBook { get; } = new();

            public SfcdCharSlotDef(string slotId, string defaultName)
            {
                SlotId = slotId;
                DefaultName = defaultName;
            }

            public bool TryGetOffset(ShiningForceCDBook book, out int offset)
                => OffsetByBook.TryGetValue(book, out offset);

            public string GetName(ShiningForceCDBook book)
                => NameByBook.TryGetValue(book, out var n) ? n : DefaultName;
        }

    private void UpdateTitle()
    {
        const string baseTitle = "Shining Editor";
        Text = _save is null
            ? $"{baseTitle} — {_game.Title}"
            : $"{baseTitle} — {_game.Title} — {Path.GetFileName(_save.Path)}{(Dirty ? " *" : "")}";
    }
}
