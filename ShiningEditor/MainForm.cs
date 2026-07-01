using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShiningEditor
{
    public partial class MainForm : Form
    {
        #region - Enums -
        public enum AppPanel
        {
            All,
            ShiningInTheDarkness,
            ShiningForce,
            ShiningForce2,
            ShiningForceCD,
            None
        }

        public enum ShiningForceCDBook
        {
            Book1TowardsTheRootOfEvil,
            Book2TheEvilGodAwakes,
            Book3ANewChallenge,
            Book4TheLastBattle,
            UnknownBook
        }
        #endregion

        #region - Class Fields -
        private bool fileLoaded;
        private const string SHINING_GOLD_LOC = "3B1C";
        private const string SHINING_FORCE_GOLD_LOC = "C107";
        private const string SHINING_FORCE_2_GOLD_LOC = "11A7A";
        private const string SHINING_FORCE_CD_GOLD_LOC = "0E078";
        private const string SHINING_FORCE_CD_BOOK_INDEX_OFFSET = "E0E1";
        private const int SfcdCharTableBase = 0xD522;
        private const int SfcdCharSlotSize = 0x38;
        private const int SfcdNameOffset = 0x2E; // inside slot record
        private const int SfcdNameLength = 0x0A; // 10 bytes
        private const int SFCD_PLAYER_NAME_OFFSET = 0x0D518;
        private const int SFCD_PLAYER_NAME_LENGTH = 10;
        private const int SfcdClassIdOffset = 0x00;

        // One known good class-table start. If you later confirm a different offset for your format/version,
        // just change this constant.
        private const int SfcdClassTableOffset = 0x0D76A0;

        // ── Save-state file validation / backup ──────────────────────────────
        // Kega Fusion save states begin with the ASCII signature "GST". The RAM
        // dump size distinguishes the console: Genesis (SID/SF/SF2) vs Sega CD (SFCD).
        private static readonly byte[] GST_MAGIC = { 0x47, 0x53, 0x54 }; // "GST"
        private const long GST_SIZE_GENESIS = 140408;
        private const long GST_SIZE_SEGA_CD = 1120235;

        // Files backed up (once) this session, so we don't re-snapshot on every edit.
        private readonly HashSet<string> _backedUpThisSession =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool FileLoaded
        {
            get { return fileLoaded; }
            set { fileLoaded = value; }
        }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public AppPanel ActivePanel
        {
            get { return activePanel; }
            set { activePanel = value; }
        }
        #endregion

        #region - Class Constructor -
        public MainForm()
        {
            InitializeComponent();
        }
        #endregion

        #region - Event Handlers -
        private void MainForm_Load(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.None;
            ShowPanel(AppPanel.All, false);
            FileLoaded = false;
            WireDigitOnlyInputs();
            SetUpdateButtonsEnabled(false);   // nothing to update until a file is loaded
        }

        // ── Input safety helpers ─────────────────────────────────────────────
        // Restrict every "New …" stat/gold field to digits, so a non-numeric value
        // can't be typed in the first place.
        private void WireDigitOnlyInputs()
        {
            foreach (TextBox tb in AllControls(this).OfType<TextBox>())
            {
                if (tb.Name.Contains("New") && tb.Name.EndsWith("Tb"))
                {
                    tb.KeyPress -= DigitsOnly_KeyPress;   // avoid double-wiring
                    tb.KeyPress += DigitsOnly_KeyPress;
                }
            }
        }

        private static IEnumerable<Control> AllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (Control d in AllControls(c))
                {
                    yield return d;
                }
            }
        }

        private void DigitsOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            // allow control chars (backspace, etc.) and digits only
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void SetUpdateButtonsEnabled(bool enabled)
        {
            updSaveStateBtn.Enabled = enabled;
            updShiningForceSaveStateBtn.Enabled = enabled;
            shiningForce2UpdateSaveStateBtn.Enabled = enabled;
            shiningForceCDUpdateSaveStateBtn.Enabled = enabled;
        }

        /// <summary>
        /// Reads a "New …" field. Blank = leave unchanged (returns false, no message).
        /// A non-numeric or out-of-range entry shows one clear message and returns false,
        /// so an over-large value can't silently truncate to the field's byte width.
        /// </summary>
        private bool TryReadField(TextBox tb, string label, long min, long max, out long value)
        {
            value = 0;
            string text = tb.Text.Trim();
            if (text.Length == 0)
            {
                return false;
            }

            if (!long.TryParse(text, out value) || value < min || value > max)
            {
                value = 0;
                MessageBox.Show($"{label} must be a whole number from {min:N0} to {max:N0}.",
                    "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void shiningInTheDarknessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningInTheDarkness;
            saveStateFileTb.Text = string.Empty;
            SetUpdateButtonsEnabled(false);
            ResetShiningControls(true, true);
            PopulateShiningCharacterList();
            ShowPanel(AppPanel.ShiningInTheDarkness, true);
        }

        private void shiningForceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningForce;
            saveStateFileTb.Text = string.Empty;
            SetUpdateButtonsEnabled(false);
            ResetShiningForceControls(true);
            PopulateShiningForceCharacterList();
            ShowPanel(AppPanel.ShiningForce, true);
        }

        private void shiningForce2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningForce2;
            saveStateFileTb.Text = string.Empty;
            SetUpdateButtonsEnabled(false);
            ResetShiningForce2Controls(true);
            PopulateShiningForce2CharacterList();
            ShowPanel(AppPanel.ShiningForce2, true);
        }

        private void shiningForceCDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivePanel = AppPanel.ShiningForceCD;
            saveStateFileTb.Text = string.Empty;
            SetUpdateButtonsEnabled(false);
            ResetShiningForceCDControls(true, true);            
            ShowPanel(AppPanel.ShiningForceCD, true);
        }

        private void viewErrorLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ErrorLogView errLogView = new ErrorLogView();
            errLogView.ShowDialog();
        }

        private void clearErrorLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearErrorLog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void browseBtn_Click(object sender, EventArgs e)
        {
            string game = GetSelectedGameTitle();
            if (game != string.Empty)
            {
                // Set the open file dialog properties
                openFD.Title = $"Select a {game} save state file";
                openFD.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                openFD.FileName = "";

                // Show the open file dialog and capture the selected file
                if (openFD.ShowDialog() != DialogResult.Cancel)
                {
                    if (!ValidateSaveStateFile(openFD.FileName, out string validationError))
                    {
                        MessageBox.Show(validationError, "Invalid save state file",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        saveStateFileTb.Text = string.Empty;
                        FileLoaded = false;
                        return;
                    }

                    saveStateFileTb.Text = openFD.FileName;
                    FileLoaded = true;
                    EnsureBufferLoaded();   // load the whole file into memory once
                    SetUpdateButtonsEnabled(true);
                    switch (ActivePanel)
                    {
                        case AppPanel.ShiningInTheDarkness:
                            PopulateShiningCurrentGold();
                            break;

                        case AppPanel.ShiningForce:
                            PopulateShiningForceCurrentGold();
                            PopulateShiningForceItemsList();
                            PopulateShiningForceMagicList();
                            break;

                        case AppPanel.ShiningForce2:
                            PopulateShiningForce2CurrentGold();
                            PopulateShiningForce2ItemsList();
                            PopulateShiningForce2MagicList();
                            break;

                        case AppPanel.ShiningForceCD:
                            ResetSfcdCaches();
                            DetermineShiningForceCDCurrentBook();
                            shiningForceCDCurrentBookTb.Text = GetShiningForceCDCurrentBookString();

                            PopulateShiningForceCDCurrentGold();                            
                            PopulateShiningForceCDItemsList(_shiningForceCDBook);
                            PopulateShiningForceCDMagicList();
                            PopulateShiningForceCDCharacterList(false);
                            break;
                    }
                }
                else
                {
                    saveStateFileTb.Text = string.Empty;
                }
            }
            else
            {
                MessageBox.Show("You must first select a game from the menu so the correct game data is loaded.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void shiningCharacterCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (shiningCharacterCmb.SelectedIndex >= 0)
            {
                if (FileLoaded)
                {
                    ResetShiningControls(false, false);
                    PopulateShiningCharacterDetails(shiningCharacterCmb.SelectedItem as ShiningCharacterItem);
                }
                else
                {
                    MessageBox.Show("You must load a save state file before you can view character data.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void shiningForceCharacterCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (shiningForceCharacterCmb.SelectedIndex >= 0)
            {
                if (FileLoaded)
                {
                    PopulateShiningForceCharacterDetails(shiningForceCharacterCmb.SelectedItem as ShiningForceCharacterItem);
                }
                else
                {
                    MessageBox.Show("You must load a save state file before you can view character data.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void shiningForce2CharacterCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (shiningForce2CharacterCmb.SelectedIndex >= 0)
            {
                if (FileLoaded)
                {
                    PopulateShiningForce2CharacterDetails(shiningForce2CharacterCmb.SelectedItem as ShiningForce2CharacterItem);
                }
                else
                {
                    MessageBox.Show("You must load a save state file before you can view character data.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void shiningForceCDSelectCharacterCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (shiningForceCDSelectCharacterCmb.SelectedIndex >= 0)
            {
                if (FileLoaded)
                {
                    ShiningForceCDCharacterItem selectedCharacter = shiningForceCDSelectCharacterCmb.SelectedItem as ShiningForceCDCharacterItem;
                    if (selectedCharacter == null)
                    {
                        return;
                    }

                    PopulateShiningForceCDCharacterDetails(selectedCharacter);
                }
                else
                {
                    MessageBox.Show("You must load a save state file before you can view character data.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void updSaveStateBtn_Click(object sender, EventArgs e)
        {
            UpdateShiningSaveState();
        }

        private void updShiningForceSaveStateBtn_Click(object sender, EventArgs e)
        {
            UpdateShiningForceSaveState();
        }

        private void shiningForce2UpdateSaveStateBtn_Click(object sender, EventArgs e)
        {
            UpdateShiningForce2SaveState();
        }

        private void shiningForceCDUpdateSaveStateBtn_Click(object sender, EventArgs e)
        {
            UpdateShiningForceCDSaveState();
        }
        #endregion

        #region - Private Methods -
        private void ShowPanel(AppPanel panel, bool show)
        {
            switch (panel)
            {
                case AppPanel.All:
                    shiningPanel.Visible = show;
                    shiningForcePanel.Visible = show;
                    shiningForce2Panel.Visible = show;
                    shiningForceCDPanel.Visible = show;
                    break;

                case AppPanel.ShiningInTheDarkness:
                    shiningPanel.Visible = show;
                    if (show)
                    {
                        shiningForcePanel.Visible = !show;
                        shiningForce2Panel.Visible = !show;
                        shiningForceCDPanel.Visible = !show;
                    }
                    break;

                case AppPanel.ShiningForce:
                    shiningForcePanel.Visible = show;
                    if (show)
                    {
                        shiningPanel.Visible = !show;
                        shiningForce2Panel.Visible = !show;
                        shiningForceCDPanel.Visible = !show;
                    }
                    break;

                case AppPanel.ShiningForce2:
                    shiningForce2Panel.Visible = show;
                    if (show)
                    {
                        shiningPanel.Visible = !show;
                        shiningForcePanel.Visible = !show;
                        shiningForceCDPanel.Visible = !show;
                    }
                    break;

                case AppPanel.ShiningForceCD:
                    shiningForceCDPanel.Visible = show;
                    if (show)
                    {
                        shiningPanel.Visible = !show;
                        shiningForcePanel.Visible = !show;
                        shiningForce2Panel.Visible = !show;
                    }
                    break;
            }
        }

        private void ShowControl(TextBox control, bool show)
        {
            control.Visible = show;
        }        

        private void ResetShiningControls(bool resetCharacterList = false, bool resetGold = false)
        {
            if (resetCharacterList)
            {
                shiningCharacterCmb.SelectedIndex = -1;
            }

            if (resetGold)
            {
                shiningCurGoldTb.Text = string.Empty;
                shiningNewGoldTb.Text = string.Empty;
            }
            
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
            shiningCharacterCmb.Items.Clear();

            ShiningCharacterItem hiroItem = new ShiningCharacterItem("Hiro",
                "3B50",
                "3B56",
                "3B20",
                "3B2C",
                "3B26",
                "3B32",
                "3B4A",
                "398A",
                "3990",
                "3B38",
                "3978");
            shiningCharacterCmb.Items.Add(hiroItem);

            ShiningCharacterItem miloItem = new ShiningCharacterItem("Milo",
                "3B52",
                "3B5A",
                "3B22",
                "3B2E",
                "3B28",
                "3B34",
                "3B4C",
                "398C",
                "3992",
                "3B3A",
                "397A");
            shiningCharacterCmb.Items.Add(miloItem);

            ShiningCharacterItem pyraItem = new ShiningCharacterItem("Pyra",
                "3B54",
                "3B5E",
                "3B24",
                "3B30",
                "3B2A",
                "3B36",
                "3B4E",
                "398E",
                "3994",
                "3B3C",
                "397C");
            shiningCharacterCmb.Items.Add(pyraItem);

            shiningCharacterCmb.DisplayMember = "Name";
        }

        private string GetSelectedGameTitle()
        {
            string game = string.Empty;
            switch (ActivePanel)
            {
                case AppPanel.ShiningInTheDarkness:
                    game = "Shining in the Darkness";
                    break;

                case AppPanel.ShiningForce:
                    game = "Shining Force";
                    break;

                case AppPanel.ShiningForce2:
                    game = "Shining Force 2";
                    break;

                case AppPanel.ShiningForceCD:
                    game = "Shining Force CD";
                    break;
            }

            return game;
        }

        private string GetShiningCurrentGold()
        {
            string hexVal = GetValueByOffset(SHINING_GOLD_LOC, 4);
            long gold = long.Parse(hexVal, System.Globalization.NumberStyles.HexNumber);
            return gold.ToString();
        }

        private void PopulateShiningCurrentGold()
        {
            shiningCurGoldTb.Text = GetShiningCurrentGold();
        }

        private void PopulateShiningCharacterDetails(ShiningCharacterItem charItem)
        {
            string value = GetValueByOffset(charItem.LevelLoc, 2);
            long val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningLevelTb.Text = val.ToString();
            value = GetValueByOffset(charItem.ExpLoc, 4);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningExpTb.Text = val.ToString();
            value = GetValueByOffset(charItem.CurHPLoc, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningCurHPTb.Text = val.ToString();
            value = GetValueByOffset(charItem.MaxHPLoc, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningMaxHPTb.Text = val.ToString();

            if (charItem.Name != "Hiro")
            {
                ShowControl(shiningNewCurMPTb, true);
                ShowControl(shiningNewMaxMPTb, true);
                value = GetValueByOffset(charItem.CurMPLoc, 2);
                val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
                shiningCurMPTb.Text = val.ToString();
                value = GetValueByOffset(charItem.MaxMPLoc, 2);
                val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
                shiningMaxMPTb.Text = val.ToString();
            }
            else
            {
                ShowControl(shiningNewCurMPTb, false);
                ShowControl(shiningNewMaxMPTb, false);
            }

            value = GetValueByOffset(charItem.IQLoc, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningIQTb.Text = val.ToString();
            value = GetValueByOffset(charItem.SpeedLoc, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningSpeedTb.Text = val.ToString();
            value = GetValueByOffset(charItem.LuckLoc, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningLuckTb.Text = val.ToString();
            value = GetValueByOffset(charItem.AttackLoc, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningAttackTb.Text = val.ToString();
            value = GetValueByOffset(charItem.DefLoc, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningDefTb.Text = val.ToString();
        }

        private string GetShiningForceCurrentGold()
        {
            string hexVal = GetValueByOffset(SHINING_FORCE_GOLD_LOC, 3);
            long gold = long.Parse(hexVal, System.Globalization.NumberStyles.HexNumber);
            return gold.ToString();
        }

        private void PopulateShiningForceCurrentGold()
        {
            shiningForceCurGoldTb.Text = GetShiningForceCurrentGold();
        }

        private void ResetShiningForceControls(bool resetCharacterList)
        {
            if (resetCharacterList)
            {
                shiningForceCharacterCmb.SelectedIndex = -1;
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

        private ShiningForceCharacterItem MakeSfChar(string name, int b) =>
            new ShiningForceCharacterItem(
                name,
                Hex(b + 0x00), Hex(b + 0x01), Hex(b + 0x02), Hex(b + 0x03), Hex(b + 0x04),
                Hex(b + 0x06),                                   // experience
                Hex(b + 0x08), Hex(b + 0x0A),                    // max HP, current HP
                Hex(b + 0x0B), Hex(b + 0x0C),                    // current MP, max MP
                new[] { Hex(b + 0x0F), Hex(b + 0x10), Hex(b + 0x11), Hex(b + 0x12) },
                new[] { Hex(b + 0x13), Hex(b + 0x14), Hex(b + 0x15), Hex(b + 0x16) });

        private void PopulateShiningForceCharacterList()
        {
            shiningForceCharacterCmb.Items.Clear();
            shiningForceCharacterCmb.DisplayMember = "Name";

            // Each character record is 0x28 bytes; fields sit at fixed sub-offsets.
            string[] names =
            {
                "Hero", "Mae", "Pelle", "Ken", "Vankar", "Earnest", "Aurthur", "Gort", "Luke",
                "Guntz", "Anri", "Alef", "Tao", "Domingo", "Lowe", "Khris", "Torasu", "Gong",
                "Diane", "Hans", "Lyle", "Amon", "Balbaroy", "Kokichi", "Bleu", "Adam", "Zylo",
                "Musashi", "Hanzou", "Jogurt"
            };

            for (int i = 0; i < names.Length; i++)
            {
                shiningForceCharacterCmb.Items.Add(MakeSfChar(names[i], 0xC115 + i * 0x28));
            }
        }

        private void PopulateShiningForceItemsList()
        {
            shiningForceItemsList = new List<ShiningForceItem>();
            shiningForceItemsList.Add(new ShiningForceItem("Medical Herb", "00"));
            shiningForceItemsList.Add(new ShiningForceItem("Healing Seed", "01"));
            shiningForceItemsList.Add(new ShiningForceItem("Antidote", "02"));
            shiningForceItemsList.Add(new ShiningForceItem("Show of Cure", "03"));
            shiningForceItemsList.Add(new ShiningForceItem("Angel Wing", "04"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Potion", "05"));
            shiningForceItemsList.Add(new ShiningForceItem("Defense Potion", "06"));
            shiningForceItemsList.Add(new ShiningForceItem("Legs of Haste", "07"));
            shiningForceItemsList.Add(new ShiningForceItem("Turbo Pepper", "08"));
            shiningForceItemsList.Add(new ShiningForceItem("Bread of Life", "09"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Ring", "0A"));
            shiningForceItemsList.Add(new ShiningForceItem("Shield Ring", "0B"));
            shiningForceItemsList.Add(new ShiningForceItem("Speed Ring", "0C"));
            shiningForceItemsList.Add(new ShiningForceItem("Mobility Ring", "0D"));
            shiningForceItemsList.Add(new ShiningForceItem("White Ring", "0E"));
            shiningForceItemsList.Add(new ShiningForceItem("Black Ring", "0F"));
            shiningForceItemsList.Add(new ShiningForceItem("Evil Ring", "10"));
            shiningForceItemsList.Add(new ShiningForceItem("Sugoi Mizugi", "11"));
            shiningForceItemsList.Add(new ShiningForceItem("Orb of Light", "12"));
            shiningForceItemsList.Add(new ShiningForceItem("Moon Stone", "13"));
            shiningForceItemsList.Add(new ShiningForceItem("Lunar Dew", "14"));
            shiningForceItemsList.Add(new ShiningForceItem("Kutui Huku", "15"));
            shiningForceItemsList.Add(new ShiningForceItem("Domingo Egg", "16"));
            shiningForceItemsList.Add(new ShiningForceItem("Kenji", "17"));
            shiningForceItemsList.Add(new ShiningForceItem("Teppou", "18"));
            shiningForceItemsList.Add(new ShiningForceItem("Kaku-Chan", "19"));
            shiningForceItemsList.Add(new ShiningForceItem("Yougi", "1A"));
            shiningForceItemsList.Add(new ShiningForceItem("Great Axe", "1B"));
            shiningForceItemsList.Add(new ShiningForceItem("Kinden No Hako", "1C"));
            shiningForceItemsList.Add(new ShiningForceItem("Short Sword", "1D"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Sword", "1E"));
            shiningForceItemsList.Add(new ShiningForceItem("Long Sword", "1F"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Sword", "20"));
            shiningForceItemsList.Add(new ShiningForceItem("Broad Sword", "21"));
            shiningForceItemsList.Add(new ShiningForceItem("Doom Blade", "22"));
            shiningForceItemsList.Add(new ShiningForceItem("Katana", "23"));
            shiningForceItemsList.Add(new ShiningForceItem("Elven Arrow", "24"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Darkness", "25"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Light", "26"));
            shiningForceItemsList.Add(new ShiningForceItem("Chaos Breaker", "27"));
            shiningForceItemsList.Add(new ShiningForceItem("Bronze Lance", "28"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Lance", "29"));
            shiningForceItemsList.Add(new ShiningForceItem("Chrome Lance", "2A"));
            shiningForceItemsList.Add(new ShiningForceItem("Devil Lance", "2B"));
            shiningForceItemsList.Add(new ShiningForceItem("Halberd", "2C"));
            shiningForceItemsList.Add(new ShiningForceItem("Spear", "2D"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Spear", "2E"));
            shiningForceItemsList.Add(new ShiningForceItem("Valkyrie", "2F"));
            shiningForceItemsList.Add(new ShiningForceItem("Hand Axe", "30"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Axe", "31"));
            shiningForceItemsList.Add(new ShiningForceItem("Battle Axe", "32"));
            shiningForceItemsList.Add(new ShiningForceItem("Heat Axe", "33"));
            shiningForceItemsList.Add(new ShiningForceItem("Atlas", "34"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Staff", "35"));
            shiningForceItemsList.Add(new ShiningForceItem("Guardian Staff", "36"));
            shiningForceItemsList.Add(new ShiningForceItem("Holy Staff", "37"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Staff", "38"));
            shiningForceItemsList.Add(new ShiningForceItem("Demon Rod", "39"));
            shiningForceItemsList.Add(new ShiningForceItem("Yogurt Ring", "3A"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Arrow", "3B"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Arrow", "3C"));
            shiningForceItemsList.Add(new ShiningForceItem("Assault Shell", "3D"));
            shiningForceItemsList.Add(new ShiningForceItem("Buster Shot", "3E"));
            shiningForceItemsList.Add(new ShiningForceItem("Dummy", "3F"));
            shiningForceItemsList.Add(new ShiningForceItem("Medical Herb", "40"));
            shiningForceItemsList.Add(new ShiningForceItem("Healing Seed", "41"));
            shiningForceItemsList.Add(new ShiningForceItem("Antidote", "42"));
            shiningForceItemsList.Add(new ShiningForceItem("Shower of Cure", "43"));
            shiningForceItemsList.Add(new ShiningForceItem("Angel Wing", "44"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Potion", "45"));
            shiningForceItemsList.Add(new ShiningForceItem("Defense Potion", "46"));
            shiningForceItemsList.Add(new ShiningForceItem("Legs of Haste", "47"));
            shiningForceItemsList.Add(new ShiningForceItem("Turbo Pepper", "48"));
            shiningForceItemsList.Add(new ShiningForceItem("Bread of Life", "49"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Ring", "4A"));
            shiningForceItemsList.Add(new ShiningForceItem("Shield Ring", "4B"));
            shiningForceItemsList.Add(new ShiningForceItem("Speed Ring", "4C"));
            shiningForceItemsList.Add(new ShiningForceItem("Mobility Ring", "4D"));
            shiningForceItemsList.Add(new ShiningForceItem("White Ring", "4E"));
            shiningForceItemsList.Add(new ShiningForceItem("Black Ring", "4F"));
            shiningForceItemsList.Add(new ShiningForceItem("Evil Ring", "50"));
            shiningForceItemsList.Add(new ShiningForceItem("Sugoi Muzugi", "51"));
            shiningForceItemsList.Add(new ShiningForceItem("Orb of Light", "52"));
            shiningForceItemsList.Add(new ShiningForceItem("Moon Stone", "53"));
            shiningForceItemsList.Add(new ShiningForceItem("Lunar Dew", "54"));
            shiningForceItemsList.Add(new ShiningForceItem("Kitui Huku", "55"));
            shiningForceItemsList.Add(new ShiningForceItem("Domingo Egg", "56"));
            shiningForceItemsList.Add(new ShiningForceItem("Kenji", "57"));
            shiningForceItemsList.Add(new ShiningForceItem("Teppou", "58"));
            shiningForceItemsList.Add(new ShiningForceItem("Kaku-Chan", "59"));
            shiningForceItemsList.Add(new ShiningForceItem("Yougi", "5A"));
            shiningForceItemsList.Add(new ShiningForceItem("Great Axe", "5B"));
            shiningForceItemsList.Add(new ShiningForceItem("Kindan No Hako", "5C"));
            shiningForceItemsList.Add(new ShiningForceItem("Short Sword", "5D"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Sword", "5E"));
            shiningForceItemsList.Add(new ShiningForceItem("Long Sword", "5F"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Sword", "60"));
            shiningForceItemsList.Add(new ShiningForceItem("Broad Sword", "61"));
            shiningForceItemsList.Add(new ShiningForceItem("Doom Blade", "62"));
            shiningForceItemsList.Add(new ShiningForceItem("Katana", "63"));
            shiningForceItemsList.Add(new ShiningForceItem("Elven Arrow", "64"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Darkness", "65"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Light", "66"));
            shiningForceItemsList.Add(new ShiningForceItem("Chaos Breaker", "67"));
            shiningForceItemsList.Add(new ShiningForceItem("Bronze Lance", "68"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Lance", "69"));
            shiningForceItemsList.Add(new ShiningForceItem("Chrome Lance", "6A"));
            shiningForceItemsList.Add(new ShiningForceItem("Devil Lance", "6B"));
            shiningForceItemsList.Add(new ShiningForceItem("Halberd", "6C"));
            shiningForceItemsList.Add(new ShiningForceItem("Spear", "6D"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Spear", "6E"));
            shiningForceItemsList.Add(new ShiningForceItem("Valkyrie", "6F"));
            shiningForceItemsList.Add(new ShiningForceItem("Hand Axe", "70"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Axe", "71"));
            shiningForceItemsList.Add(new ShiningForceItem("Battle Axe", "72"));
            shiningForceItemsList.Add(new ShiningForceItem("Heat Axe", "73"));
            shiningForceItemsList.Add(new ShiningForceItem("Atlas", "74"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Staff", "75"));
            shiningForceItemsList.Add(new ShiningForceItem("Guardian Staff", "76"));
            shiningForceItemsList.Add(new ShiningForceItem("Holy Staff", "77"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Staff", "78"));
            shiningForceItemsList.Add(new ShiningForceItem("Demon Rod", "79"));
            shiningForceItemsList.Add(new ShiningForceItem("Yogurt Ring", "7A"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Arrow", "7B"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Arrow", "7C"));
            shiningForceItemsList.Add(new ShiningForceItem("Assault Shell", "7D"));
            shiningForceItemsList.Add(new ShiningForceItem("Buster Shot", "7E"));
            shiningForceItemsList.Add(new ShiningForceItem("Dummy", "7F"));
            shiningForceItemsList.Add(new ShiningForceItem("Medical Herb", "80"));
            shiningForceItemsList.Add(new ShiningForceItem("Healing Seed", "81"));
            shiningForceItemsList.Add(new ShiningForceItem("Antidote", "82"));
            shiningForceItemsList.Add(new ShiningForceItem("Show of Cure", "83"));
            shiningForceItemsList.Add(new ShiningForceItem("Angel Wing", "84"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Potion", "85"));
            shiningForceItemsList.Add(new ShiningForceItem("Defense Potion", "86"));
            shiningForceItemsList.Add(new ShiningForceItem("Legs of Haste", "87"));
            shiningForceItemsList.Add(new ShiningForceItem("Turbo Pepper", "88"));
            shiningForceItemsList.Add(new ShiningForceItem("Bread of Life", "89"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Ring", "8A"));
            shiningForceItemsList.Add(new ShiningForceItem("Shield Ring", "8B"));
            shiningForceItemsList.Add(new ShiningForceItem("Speed Ring", "8C"));
            shiningForceItemsList.Add(new ShiningForceItem("Mobility Ring", "8D"));
            shiningForceItemsList.Add(new ShiningForceItem("White Ring", "8E"));
            shiningForceItemsList.Add(new ShiningForceItem("Black Ring", "8F"));
            shiningForceItemsList.Add(new ShiningForceItem("Evil Ring", "90"));
            shiningForceItemsList.Add(new ShiningForceItem("Sugoi Mizugi", "91"));
            shiningForceItemsList.Add(new ShiningForceItem("Orb of Light", "92"));
            shiningForceItemsList.Add(new ShiningForceItem("Moon Stone", "93"));
            shiningForceItemsList.Add(new ShiningForceItem("Lunar Dew", "94"));
            shiningForceItemsList.Add(new ShiningForceItem("Kutui Huku", "95"));
            shiningForceItemsList.Add(new ShiningForceItem("Domingo Egg", "96"));
            shiningForceItemsList.Add(new ShiningForceItem("Kenji", "97"));
            shiningForceItemsList.Add(new ShiningForceItem("Teppou", "98"));
            shiningForceItemsList.Add(new ShiningForceItem("Kaku-Chan", "99"));
            shiningForceItemsList.Add(new ShiningForceItem("Yougi", "9A"));
            shiningForceItemsList.Add(new ShiningForceItem("Great Axe", "9B"));
            shiningForceItemsList.Add(new ShiningForceItem("Kinden No Hako", "9C"));
            shiningForceItemsList.Add(new ShiningForceItem("Short Sword", "9D"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Sword", "9E"));
            shiningForceItemsList.Add(new ShiningForceItem("Long Sword", "9F"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Sword", "A0"));
            shiningForceItemsList.Add(new ShiningForceItem("Broad Sword", "A1"));
            shiningForceItemsList.Add(new ShiningForceItem("Doom Blade", "A2"));
            shiningForceItemsList.Add(new ShiningForceItem("Katana", "A3"));
            shiningForceItemsList.Add(new ShiningForceItem("Elven Arrow", "A4"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Darkness", "A5"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Light", "A6"));
            shiningForceItemsList.Add(new ShiningForceItem("Chaos Breaker", "A7"));
            shiningForceItemsList.Add(new ShiningForceItem("Bronze Lance", "A8"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Lance", "A9"));
            shiningForceItemsList.Add(new ShiningForceItem("Chrome Lance", "AA"));
            shiningForceItemsList.Add(new ShiningForceItem("Devil Lance", "AB"));
            shiningForceItemsList.Add(new ShiningForceItem("Halberd", "AC"));
            shiningForceItemsList.Add(new ShiningForceItem("Spear", "AD"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Spear", "AE"));
            shiningForceItemsList.Add(new ShiningForceItem("Valkyrie", "AF"));
            shiningForceItemsList.Add(new ShiningForceItem("Hand Axe", "B0"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Axe", "B1"));
            shiningForceItemsList.Add(new ShiningForceItem("Battle Axe", "B2"));
            shiningForceItemsList.Add(new ShiningForceItem("Heat Axe", "B3"));
            shiningForceItemsList.Add(new ShiningForceItem("Atlas", "B4"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Staff", "B5"));
            shiningForceItemsList.Add(new ShiningForceItem("Guardian Staff", "B6"));
            shiningForceItemsList.Add(new ShiningForceItem("Holy Staff", "B7"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Staff", "B8"));
            shiningForceItemsList.Add(new ShiningForceItem("Demon Rod", "B9"));
            shiningForceItemsList.Add(new ShiningForceItem("Yogurt Ring", "BA"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Arrow", "BB"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Arrow", "BC"));
            shiningForceItemsList.Add(new ShiningForceItem("Assault Shell", "BD"));
            shiningForceItemsList.Add(new ShiningForceItem("Buster Shot", "BE"));
            shiningForceItemsList.Add(new ShiningForceItem("Empty", "BF"));
            shiningForceItemsList.Add(new ShiningForceItem("Medical Herb", "C0"));
            shiningForceItemsList.Add(new ShiningForceItem("Healing Seed", "C1"));
            shiningForceItemsList.Add(new ShiningForceItem("Antidote", "C2"));
            shiningForceItemsList.Add(new ShiningForceItem("Shower of Cure", "C3"));
            shiningForceItemsList.Add(new ShiningForceItem("Angel Wing", "C4"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Potion", "C5"));
            shiningForceItemsList.Add(new ShiningForceItem("Defense Potion", "C6"));
            shiningForceItemsList.Add(new ShiningForceItem("Legs of Haste", "C7"));
            shiningForceItemsList.Add(new ShiningForceItem("Turbo Pepper", "C8"));
            shiningForceItemsList.Add(new ShiningForceItem("Bread of Life", "C9"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Ring", "CA"));
            shiningForceItemsList.Add(new ShiningForceItem("Shield Ring", "CB"));
            shiningForceItemsList.Add(new ShiningForceItem("Speed Ring", "CC"));
            shiningForceItemsList.Add(new ShiningForceItem("Mobility Ring", "CD"));
            shiningForceItemsList.Add(new ShiningForceItem("White Ring", "CE"));
            shiningForceItemsList.Add(new ShiningForceItem("Black Ring", "CF"));
            shiningForceItemsList.Add(new ShiningForceItem("Evil Ring", "D0"));
            shiningForceItemsList.Add(new ShiningForceItem("Sugoi Muzugi", "D1"));
            shiningForceItemsList.Add(new ShiningForceItem("Orb of Light", "D2"));
            shiningForceItemsList.Add(new ShiningForceItem("Moon Stone", "D3"));
            shiningForceItemsList.Add(new ShiningForceItem("Lunar Dew", "D4"));
            shiningForceItemsList.Add(new ShiningForceItem("Kitui Huku", "D5"));
            shiningForceItemsList.Add(new ShiningForceItem("Domingo Egg", "D6"));
            shiningForceItemsList.Add(new ShiningForceItem("Kenji", "D7"));
            shiningForceItemsList.Add(new ShiningForceItem("Teppou", "D8"));
            shiningForceItemsList.Add(new ShiningForceItem("Kaku-Chan", "D9"));
            shiningForceItemsList.Add(new ShiningForceItem("Yougi", "DA"));
            shiningForceItemsList.Add(new ShiningForceItem("Great Axe", "DB"));
            shiningForceItemsList.Add(new ShiningForceItem("Kindan No Hako", "DC"));
            shiningForceItemsList.Add(new ShiningForceItem("Short Sword", "DD"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Sword", "DE"));
            shiningForceItemsList.Add(new ShiningForceItem("Long Sword", "DF"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Sword", "E0"));
            shiningForceItemsList.Add(new ShiningForceItem("Broad Sword", "E1"));
            shiningForceItemsList.Add(new ShiningForceItem("Doom Blade", "E2"));
            shiningForceItemsList.Add(new ShiningForceItem("Katana", "E3"));
            shiningForceItemsList.Add(new ShiningForceItem("Elven Arrow", "E4"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Darkness", "E5"));
            shiningForceItemsList.Add(new ShiningForceItem("Sword of Light", "E6"));
            shiningForceItemsList.Add(new ShiningForceItem("Chaos Breaker", "E7"));
            shiningForceItemsList.Add(new ShiningForceItem("Bronze Lance", "E8"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Lance", "E9"));
            shiningForceItemsList.Add(new ShiningForceItem("Chrome Lance", "EA"));
            shiningForceItemsList.Add(new ShiningForceItem("Devil Lance", "EB"));
            shiningForceItemsList.Add(new ShiningForceItem("Halberd", "EC"));
            shiningForceItemsList.Add(new ShiningForceItem("Spear", "ED"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Spear", "EE"));
            shiningForceItemsList.Add(new ShiningForceItem("Valkyrie", "EF"));
            shiningForceItemsList.Add(new ShiningForceItem("Hand Axe", "F0"));
            shiningForceItemsList.Add(new ShiningForceItem("Middle Axe", "F1"));
            shiningForceItemsList.Add(new ShiningForceItem("Battle Axe", "F2"));
            shiningForceItemsList.Add(new ShiningForceItem("Heat Axe", "F3"));
            shiningForceItemsList.Add(new ShiningForceItem("Atlas", "F4"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Staff", "F5"));
            shiningForceItemsList.Add(new ShiningForceItem("Guardian Staff", "F6"));
            shiningForceItemsList.Add(new ShiningForceItem("Holy Staff", "F7"));
            shiningForceItemsList.Add(new ShiningForceItem("Power Staff", "F8"));
            shiningForceItemsList.Add(new ShiningForceItem("Demon Rod", "F9"));
            shiningForceItemsList.Add(new ShiningForceItem("Yogurt Ring", "FA"));
            shiningForceItemsList.Add(new ShiningForceItem("Wooden Arrow", "FB"));
            shiningForceItemsList.Add(new ShiningForceItem("Steel Arrow", "FC"));
            shiningForceItemsList.Add(new ShiningForceItem("Assault Shell", "FD"));
            shiningForceItemsList.Add(new ShiningForceItem("Buster Shot", "FE"));
            shiningForceItemsList.Add(new ShiningForceItem("Empty", "FF"));
        }

        private void PopulateShiningForceMagicList()
        {
            shiningForceMagicList = new List<ShiningForceMagicItem>();
            shiningForceMagicList.Add(new ShiningForceMagicItem("Heal 1", "00"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Aura 1", "01"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Detox 1", "02"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Quick 1", "03"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Slow 1", "04"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Boost 1", "05"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dispel 1", "06"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Shield 1", "07"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Muddle 1", "08"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Blaze 1", "09"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Freeze 1", "0A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Bolt 1", "0B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Desoul 1", "0C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Egress 1", "0D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "0E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Sleep 1", "0F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "10"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "11"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "12"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "13"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "14"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "15"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "16"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "17"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "18"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "19"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "1A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "1B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "1C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "1D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "1E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "1F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "20"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "21"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "22"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "23"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "24"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "25"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "26"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "27"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "28"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "29"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "2A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "2B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "2C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "2D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "2E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "2F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "30"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "31"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "32"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "33"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "34"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "35"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "36"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "37"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "38"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "39"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "3A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "3B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "3C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "3D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "3E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy", "3F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Heal 2", "40"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Aura 2", "41"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Detox 2", "42"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Quick 2", "43"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Slow 2", "44"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Boost 2", "45"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dispel 2", "46"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Shield 2", "47"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Muddle 2", "48"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Blaze 2", "49"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Freeze 2", "4A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Bolt 2", "4B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Desoul 2", "4C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Egress 2", "4D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "4E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Sleep 2", "4F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "50"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "51"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "52"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "53"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "54"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "55"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "56"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "57"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "58"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "59"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "5A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "5B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "5C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "5D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "5E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "5F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "60"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "61"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "62"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "63"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "64"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "65"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "66"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "67"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "68"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "69"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "6A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "6B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "6C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "6D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "6E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "6F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "70"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "71"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "72"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "73"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "74"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "75"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "76"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "77"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "78"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "79"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "7A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "7B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "7C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "7D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "7E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 2", "7F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Heal 3", "80"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Aura 3", "81"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Detox 3", "82"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Quick 3", "83"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Slow 3", "84"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Boost 3", "85"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dispel 3", "86"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Shield 3", "87"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Muddle 3", "88"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Blaze 3", "89"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Freeze 3", "8A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Bolt 3", "8B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Desoul 3", "8C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Egress 3", "8D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "8E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Sleep 3", "8F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "90"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "91"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "92"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "93"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "94"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "95"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "96"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "97"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "98"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "99"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "9A"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "9B"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "9C"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "9D"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "9E"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "9F"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A0"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A1"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A2"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A3"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A4"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A5"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A6"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A7"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A8"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "A9"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "AA"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "AB"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "AC"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "AD"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "AE"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "AF"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B0"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B1"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B2"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B3"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B4"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B5"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B6"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B7"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B8"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "B9"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "BA"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "BB"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "BC"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "BD"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "BE"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 3", "BF"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Heal 4", "C0"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Aura 4", "C1"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Detox 4", "C2"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Quick 4", "C3"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Slow 4", "C4"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Boost 4", "C5"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dispel 4", "C6"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Shield 4", "C7"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Muddle 4", "C8"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Blaze 4", "C9"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Freeze 4", "CA"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Bolt 4", "CB"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Desoul 4", "CC"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Egress 4", "CD"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "CE"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Sleep 4", "CF"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D0"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D1"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D2"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D3"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D4"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D5"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D6"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D7"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D8"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "D9"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "DA"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "DB"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "DC"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "DD"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "DE"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "DF"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E0"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E1"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E2"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E3"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E4"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E5"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E6"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E7"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E8"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "E9"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "EA"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "EB"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "EC"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "ED"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "EE"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "EF"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F0"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F1"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F2"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F3"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F4"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F5"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F6"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F7"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F8"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "F9"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "FA"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "FB"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "FC"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "FD"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Dummy 4", "FE"));
            shiningForceMagicList.Add(new ShiningForceMagicItem("Empty", "FF"));
        }

        private void PopulateShiningForceCharacterDetails(ShiningForceCharacterItem characterItem)
        {
            ResetShiningForceControls(false);
            string value = GetValueByOffset(characterItem.LevelLoc, 1);
            long val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceLevelTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.AttackLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceAttackTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.DefenseLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceDefenseTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.AgilityLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceAgilityTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.MoveLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceMoveTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.ExperienceLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCurExpTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.CurrentHPLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCurHPTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.MaxHPLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceMaxHPTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.CurrentMPLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCurMPTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.MaxMPLoc, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceMaxMPTb.Text = val.ToString();

            foreach (string itemLoc in characterItem.ItemsLocs)
            {
                value = GetValueByOffset(itemLoc, 1);
                ShiningForceItem item = GetShiningForceItem(value);
                if (item != null)
                {
                    shiningForceItemsLB.Items.Add(item);
                }
            }
            shiningForceItemsLB.DisplayMember = "Name";

            foreach (string magicLoc in characterItem.MagicLocs)
            {
                value = GetValueByOffset(magicLoc, 1);
                ShiningForceMagicItem magic = GetShiningForceMagicItem(value);
                if (magic != null)
                {
                    shiningForceMagicLB.Items.Add(magic);
                }
            }
            shiningForceMagicLB.DisplayMember = "Name";
        }

        private string GetShiningForce2CurrentGold()
        {
            string hexVal = GetValueByOffset(SHINING_FORCE_2_GOLD_LOC, 2);
            long gold = long.Parse(hexVal, System.Globalization.NumberStyles.HexNumber);
            return gold.ToString();
        }

        private void PopulateShiningForce2CurrentGold()
        {
            shiningForce2CurrentGoldTb.Text = GetShiningForce2CurrentGold();
        }

        private void ResetShiningForce2Controls(bool resetCharacterList)
        {
            if (resetCharacterList)
            {
                shiningForce2CharacterCmb.SelectedIndex = -1;
            }

            shiningForce2NewGoldTb.Text = "";
            shiningForce2LevelTb.Text = "";
            shiningForce2CurAttackBaseTb.Text = "";
            shiningForce2NewAttackBaseTb.Text = "";
            shiningForce2CurAttackEquipTb.Text = "";
            shiningForce2NewAttackEquipTb.Text = "";
            shiningForce2CurDefenseBaseTb.Text = "";
            shiningForce2NewDefenseBaseTb.Text = "";
            shiningForce2CurDefenseEquipTb.Text = "";
            shiningForce2NewDefenseEquipTb.Text = "";
            shiningForce2CurAgilityBaseTb.Text = "";
            shiningForce2NewAgilityBaseTb.Text = "";
            shiningForce2CurAgilityEquipTb.Text = "";
            shiningForce2NewAgilityEquipTb.Text = "";
            shiningForce2CurMoveBaseTb.Text = "";
            shiningForce2NewMoveBaseTb.Text = "";
            shiningForce2CurMoveEquipTb.Text = "";
            shiningForce2NewMoveEquipTb.Text = "";
            shiningForce2CurExpTb.Text = "";
            shiningForce2NewExpTb.Text = "";
            shiningForce2PresentHPTb.Text = "";
            shiningForce2NewPresentHPTb.Text = "";
            shiningForce2MaxHPTb.Text = "";
            shiningForce2NewMaxHPTb.Text = "";
            shiningForce2PresentMPTb.Text = "";
            shiningForce2NewPresentMPTb.Text = "";
            shiningForce2MaxMPTb.Text = "";
            shiningForce2NewMaxMPTb.Text = "";
            shiningForce2KillsTb.Text = "";
            shiningForce2DefeatsTb.Text = "";
            shiningForce2ItemListBox.Items.Clear();
            shiningForce2MagicListBox.Items.Clear();
        }

        private ShiningForce2CharacterItem MakeSf2Char(string name, int b) =>
            new ShiningForce2CharacterItem(
                name,
                Hex(b + 0x00),                                   // level
                Hex(b + 0x07), Hex(b + 0x08),                    // attack base, equip
                Hex(b + 0x09), Hex(b + 0x0A),                    // defense base, equip
                Hex(b + 0x0B), Hex(b + 0x0C),                    // agility base, equip
                Hex(b + 0x0D), Hex(b + 0x0E),                    // move base, equip
                Hex(b + 0x25),                                   // experience
                Hex(b + 0x03), Hex(b + 0x01),                    // present HP, maximum HP
                Hex(b + 0x06), Hex(b + 0x05),                    // present MP, maximum MP
                new[] { Hex(b + 0x16), Hex(b + 0x18), Hex(b + 0x1A), Hex(b + 0x1C) },
                new[] { Hex(b + 0x1D), Hex(b + 0x1E), Hex(b + 0x1F), Hex(b + 0x20) },
                Hex(b + 0x27), Hex(b + 0x2B));                   // kills, defeats

        private void PopulateShiningForce2CharacterList()
        {
            shiningForce2CharacterCmb.Items.Clear();
            shiningForce2CharacterCmb.DisplayMember = "Name";

            // Each character record is 0x38 bytes; fields sit at fixed sub-offsets.
            string[] names =
            {
                "Bowie (Hero)", "Sarah", "Chester", "Jaha", "Kazin", "Slade", "Kiwi", "Peter",
                "May", "Gerhalt", "Luke", "Rohde", "Rick", "Elric", "Eric", "Karna", "Randolf",
                "Tyrin", "Janet", "Higins"
            };

            for (int i = 0; i < names.Length; i++)
            {
                shiningForce2CharacterCmb.Items.Add(MakeSf2Char(names[i], 0x10C83 + i * 0x38));
            }

            // Preserve original data: Bowie's 2nd item slot is at +0x15, unlike every
            // other character's +0x18 (appears to be a typo in the source data).
            ((ShiningForce2CharacterItem)shiningForce2CharacterCmb.Items[0]).ItemsOffset[1] = Hex(0x10C83 + 0x15);
        }

        private void PopulateShiningForce2ItemsList()
        {
            shiningForce2ItemsList = new List<ShiningForce2Item>();

            shiningForce2ItemsList.Add(new ShiningForce2Item("Medical Herb", "00"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Healing Seed", "01"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Healing Drop", "02"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Antidote", "03"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Angel Wing", "04"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Fairy Powder", "05"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Healing Water", "06"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Fairy Tear", "07"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Healing Rain", "08"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Power Water", "09"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Protect Milk", "0A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Quick Chicken", "0B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Running Pemento", "0C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Chearful Bread", "0D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Bright Honey", "0E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Brave Apple", "0F"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Shining Ball", "10"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Blizard", "11"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Holy Thunder", "12"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Power Ring", "13"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Protect Ring", "14"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Quick Ring", "15"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Running Ring", "16"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("White Ring", "17"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Black Ring", "18"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Evil Ring", "19"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Leather Glove", "1A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Power Glove", "1B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Brass Knuckles", "1C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Iron Knuckles", "1D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Misty Knuckles", "1E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Giant Knuckles", "1F"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Evil Knuckles", "20"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Short Axe", "21"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Hand Axe", "22"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Middle Axe", "23"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Power Axe", "24"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Battle Axe", "25"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Large Axe", "26"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Great Axe", "27"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Heat Axe", "28"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Atlas Axe", "29"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Ground Axe", "2A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Rune Axe", "2B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Evil Axe", "2C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Wooden Arrow", "2D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Iron Arrow", "2E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Steel Arrow", "2F"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Robin Arrow", "30"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Assault Shell", "31"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Great Shot", "32"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Nazca Cannon", "33"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Buster Shot", "34"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Hyper Cannon", "35"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Grand Cannon", "36"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Evil Shot", "37"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Wooden Stick", "38"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Short Sword", "39"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Middle Sword", "3A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Long Sword", "3B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Middle Sword", "3C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Achiles Sword", "3D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Broad Sword", "3E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Buster Sword", "3F"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Great Sword", "40"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Critical Sword", "41"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Battle Sword", "42"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Force Sword", "43"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Counter Sword", "44"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Levanter", "45"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Dark Sword", "46"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Wooden Sword", "47"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Short Spear", "48"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Bronze Lance", "49"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Spear", "4A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Steel Lance", "4B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Power  Spear", "4C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Heavy Lance", "4D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Javelin", "4E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Chrome Lance", "4F"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Valkyrie", "50"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Holy Lance", "51"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Mist Javelin", "52"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Halberd", "53"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Evil Lance", "54"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Wooden Rod", "55"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Short Rod", "56"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Bronze Rod", "57"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Iron Rod", "58"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Power Stick", "59"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Flail", "5A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Guardian Staff", "5B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Indra Staff", "5C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Mage Staff", "5D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Wish Staff", "5E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Great Rod", "5F"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Supply Staff", "60"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Holy Staff", "61"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Freeze Staff", "62"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Godess Staff", "63"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Mystery Staff", "64"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Demon Rod", "65"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Iron Ball", "66"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Short Knife", "67"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Dagger", "68"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Knife", "69"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Theive's Dagger", "6A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Kitana", "6B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Ninja Kitana", "6C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Gisarme", "6D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Taros Sword", "6E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Right of Hope", "6F"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Wooden Panel", "70"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Sky Orb", "71"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Cannon", "72"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Dry Stone", "73"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Dynamite", "74"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Arm of Golem", "75"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Pegasus Wing", "76"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Warrior's Pride", "77"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Silver Tank", "78"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Secret Book", "79"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Vigor Ball", "7A"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Mithryl", "7B"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Life Ring", "7C"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Cotton Balloon", "7D"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Chirrup Sandles", "7E"));
            shiningForce2ItemsList.Add(new ShiningForce2Item("Blank Space", "7F"));
        }

        private void PopulateShiningForce2MagicList()
        {
            shiningForce2MagicList = new List<ShiningForce2MagicItem>();

            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Heal1", "00"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Aura1", "01"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Detox1", "02"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Boost1", "03"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Slow1", "04"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Attack1", "05"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Dispel1", "06"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Muddle1", "07"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Desoul1", "08"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Sleep1", "09"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Egress1", "0A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blaze1", "0B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Freeze1", "0C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Bolt1", "0D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blast1", "0E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Spoit1", "0F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Healin1", "10"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Flame1", "11"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Snow1", "12"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Demon1", "13"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Power1", "14"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Guard1", "15"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Speed1", "16"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Idaten1", "17"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Health1", "18"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("B. Rock1", "19"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Laser1", "1A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Katon1", "1B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Raijin1", "1C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Dao1", "1D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Appolo1", "1E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Neptun1", "1F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Atlas1", "20"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Powder1", "21"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("G. Tear1", "22"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Hanny1", "23"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Brave1", "24"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("F. Ball1", "25"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Brezard1", "26"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Thundr", "27"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Aqua1", "28"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kiwi1", "29"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Shine1", "2A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Oddeye1", "2B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Bowie1", "2C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Sarah1", "2D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Chester1", "2E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Jaha1", "2F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kazin1", "30"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Slade1", "31"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kiwi1", "32"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Peter1", "33"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("May1", "34"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Gerhalt1", "35"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Luke1", "36"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Rohde1", "37"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Rick1", "38"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Elrick1", "39"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Eric1", "3A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Karna1", "3B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Randolf1", "3C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Tyrin1", "3D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Janet1", "3E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blank", "3F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Heal2", "40"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Aura2", "41"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Detox2", "42"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Boost2", "43"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Slow2", "44"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Attack2", "45"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Dispel2", "46"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Muddle2", "47"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Desoul2", "48"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Sleep2", "49"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Egress2", "4A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blaze2", "4B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Freeze2", "4C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Bolt2", "4D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blast2", "4E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Spoit2", "4F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Healin2", "50"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Flame2", "51"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Snow2", "52"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Demon2", "53"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Power2", "54"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Guard2", "55"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Speed2", "56"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Idaten2", "57"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Health2", "58"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("B. Rock2", "59"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Laser2", "5A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Katon2", "5B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Raijin2", "5C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Dao2", "5D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Appolo2", "5E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Neptun2", "5F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Atlas2", "60"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Powder2", "61"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("G. Tear2", "62"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Hanny2", "63"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Brave2", "64"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("F. Ball2", "65"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Brezard2", "66"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Thundr2", "67"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Aqua2", "68"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kiwi2", "69"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Shine2", "6A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Oddeye2", "6B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Bowie2", "6C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Sarah2", "6D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Chester2", "6E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Jaha2", "6F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kazin2", "70"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Slade2", "71"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kiwi2", "72"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Peter2", "73"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("May2", "74"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Gerhalt2", "75"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Luke2", "76"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Rohde2", "77"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Rick2", "78"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Elrick2", "79"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Eric2", "7A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Karna2", "7B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Randolf2", "7C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Tyrin2", "7D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Janet2", "7E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Higins2", "7F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Heal3", "80"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Aura3", "81"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Detox3", "82"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Boost3", "83"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Slow3", "84"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Attack3", "85"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Dispel3", "86"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Muddle3", "87"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Desoul3", "88"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Sleep3", "89"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Egress3", "8A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blaze3", "8B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Freeze3", "8C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Bolt3", "8D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blast3", "8E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Spoit3", "8F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Healin3", "90"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Flame3", "91"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Snow3", "92"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Demon3", "93"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Power3", "94"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Guard3", "95"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Speed3", "96"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Idaten3", "97"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Health3", "98"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("B. Rock3", "99"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Laser3", "9A"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Katon3", "9B"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Raijin3", "9C"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Dao3", "9D"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Appolo3", "9E"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Neptun3", "9F"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Atlas3", "A0"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Powder3", "A1"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("G. Tear3", "A2"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Hanny3", "A3"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Brave3", "A4"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("F. Ball3", "A5"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Brezard3", "A6"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Thundr3", "A7"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Aqua3", "A8"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kiwi3", "A9"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Shine3", "AA"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Oddeye3", "AB"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Bowie3", "AC"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Sarah3", "AD"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Chester3", "AE"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Jaha3", "AF"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kazin3", "B0"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Slade3", "B1"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Kiwi3", "B2"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Peter3", "B3"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("May3", "B4"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Gerhalt3", "B5"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Luke3", "B6"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Rohde3", "B7"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Rick3", "B8"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Elrick3", "B9"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Eric3", "BA"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Karna3", "BB"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Randolf3", "BC"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Tyrin3", "BD"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Janet3", "BE"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Higins3", "BF"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Heal4", "C0"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Aura4", "C1"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Detox4", "C2"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Boost4", "C3"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Slow4", "C4"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Attack4", "C5"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Dispel4", "C6"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Muddle4", "C7"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Desoul4", "C8"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Sleep4", "C9"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Egress4", "CA"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blaze4", "CB"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Freeze4", "CC"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Bolt4", "CD"));
            shiningForce2MagicList.Add(new ShiningForce2MagicItem("Blast4", "CE"));
        }

        private void PopulateShiningForce2CharacterDetails(ShiningForce2CharacterItem characterItem)
        {
            ResetShiningForce2Controls(false);

            string value = GetValueByOffset(characterItem.LevelOffset, 1);
            long val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2LevelTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.AttackBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurAttackBaseTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.AttackEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurAttackEquipTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.DefenseBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurDefenseBaseTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.DefenseEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurDefenseEquipTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.AgilityBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurAgilityBaseTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.AgilityEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurAgilityEquipTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.MoveBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurMoveBaseTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.MoveEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurMoveEquipTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.ExperienceOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2CurExpTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.PresentHPOffset, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2PresentHPTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.MaximumHPOffset, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2MaxHPTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.PresentMPOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2PresentMPTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.MaximumMPOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2MaxMPTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.KillsOffset, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2KillsTb.Text = val.ToString();
            value = GetValueByOffset(characterItem.DefeatsOffset, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForce2DefeatsTb.Text = val.ToString();

            foreach (string itemLoc in characterItem.ItemsOffset)
            {
                value = GetValueByOffset(itemLoc, 1);
                ShiningForce2Item item = GetShiningForce2Item(value);
                if (item != null)
                {
                    shiningForce2ItemListBox.Items.Add(item);
                }
            }
            shiningForce2ItemListBox.DisplayMember = "Name";

            foreach (string magicLoc in characterItem.MagicOffset)
            {
                value = GetValueByOffset(magicLoc, 1);
                ShiningForce2MagicItem magic = GetShiningForce2MagicItem(value);
                if (magic != null)
                {
                    shiningForce2MagicListBox.Items.Add(magic);
                }
            }
            shiningForce2MagicListBox.DisplayMember = "Name";
        }

        private void ResetShiningForceCDControls(bool resetCharacterList, bool clearGold = false)
        {
            if (resetCharacterList)
            {
                shiningForceCDSelectCharacterCmb.SelectedIndex = -1;
                shiningForceCDSelectCharacterCmb.Items.Clear();
            }

            if (clearGold)
            {
                shiningForceCDCurrentGoldTb.Text = "";
            }

            shiningForceCDNewGoldTb.Text = "";
            shiningForceCDCurrentLevelTb.Text = "";
            shiningForceCDCurrentClassTb.Text = "";
            shiningForceCDAttackBaseTb.Text = "";
            shiningForceCDNewAttackBaseTb.Text = "";
            shiningForceCDAttackEquipTb.Text = "";
            shiningForceCDNewAttackEquipTb.Text = "";
            shiningForceCDDefenseBaseTb.Text = "";
            shiningForceCDNewDefenseBaseTb.Text = "";
            shiningForceCDDefenseEquipTb.Text = "";
            shiningForceCDNewDefenseEquipTb.Text = "";
            shiningForceCDAgilityBaseTb.Text = "";
            shiningForceCDNewAgilityBaseTb.Text = "";
            shiningForceCDAgilityEquipTb.Text = "";
            shiningForceCDNewAgilityEquipTb.Text = "";
            shiningForceCDMoveBaseTb.Text = "";
            shiningForceCDNewMoveBaseTb.Text = "";
            shiningForceCDMoveEquipTb.Text = "";
            shiningForceCDNewMoveEquipTb.Text = "";
            shiningForceCDExperienceTb.Text = "";
            shiningForceCDNewExperienceTb.Text = "";
            shiningForceCDPresentHPTb.Text = "";
            shiningForceCDNewPresentHPTb.Text = "";
            shiningForceCDMaxHPTb.Text = "";
            shiningForceCDNewMaxHPTb.Text = "";
            shiningForceCDPresentMPTb.Text = "";
            shiningForceCDNewPresentMPTb.Text = "";
            shiningForceCDMaxMPTb.Text = "";
            shiningForceCDNewMaxMPTb.Text = "";
            shiningForceCDItemsListBox.Items.Clear();
            shiningForceCDMagicListBox.Items.Clear();
        }

        private string GetShiningForceCDCurrentGold()
        {
            byte[] bytes = GetBytesByOffset(SHINING_FORCE_CD_GOLD_LOC, 4);

            // Save states store gold as BIG-endian
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            uint gold = BitConverter.ToUInt32(bytes, 0);
            return gold.ToString();
        }

        private void PopulateShiningForceCDCurrentGold()
        {
            shiningForceCDCurrentGoldTb.Text = GetShiningForceCDCurrentGold();
        }

        private void PopulateShiningForceCDCharacterList()
        {
            shiningForceCDSelectCharacterCmb.Items.Clear();

            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("HERO (player)", 0x0D522)); // player name string is stored elsewhere (e.g., "Aaron" or "Able", whatever is entered.
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("NATASHA", 0x0D55A));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("ERIC", 0x0D592));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("DAWN", 0x0D5CA));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("LUKE", 0x0D602));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("SHADE", 0x0D63A));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("GRAHAM", 0x0D672));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("CHESTER", 0x0D6AA));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("MAY", 0x0D6E2));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("SARAH", 0x0D71A));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("RANDOLF", 0x0D752));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("CLAUDE", 0x0D78A));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("ROHDE", 0x0D7C2));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("RUSH", 0x0D7FA));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("HIGINS", 0x0D832));
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar("GYAN", 0x0D86A));

            shiningForceCDSelectCharacterCmb.DisplayMember = "Name";
        }

        private void PopulateShiningForceCDCharacterList(bool loadAllSlotsEvenIfBlank = false)
        {
            shiningForceCDSelectCharacterCmb.Items.Clear();

            const int slotCount = 27;

            string heroName = GetShiningForceCDPlayerName();

            // Slot 0 is HERO’s data block.
            shiningForceCDSelectCharacterCmb.Items.Add(MakeChar($"HERO ({heroName})", SfcdCharTableBase)); // 0x0D522

            // Slot 1.. are other characters. Their names are stored in the PREVIOUS slot’s name field.
            for (int slot = 1; slot < slotCount; slot++)
            {
                int statsOffset = SfcdCharTableBase + (slot * SfcdCharSlotSize);

                // Name for this slot is stored in (slot - 1)
                string name = ReadSfcdSlotNameFromStatsBase(statsOffset);

                if (!loadAllSlotsEvenIfBlank && string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string displayName = string.IsNullOrWhiteSpace(name)
                    ? $"— Empty Slot {slot} —"
                    : name;

                shiningForceCDSelectCharacterCmb.Items.Add(MakeChar(string.IsNullOrWhiteSpace(displayName) ? $"- Empty Slot {slot} -" : displayName, statsOffset));
            }

            shiningForceCDSelectCharacterCmb.DisplayMember = "Name";
        }

        private string GetShiningForceCDPlayerName()
        {
            byte[] nameBytes = GetBytesByOffset(SFCD_PLAYER_NAME_OFFSET, SFCD_PLAYER_NAME_LENGTH);

            int zeroIndex = Array.IndexOf(nameBytes, (byte)0);
            int len = zeroIndex >= 0 ? zeroIndex : SFCD_PLAYER_NAME_LENGTH;

            return System.Text.Encoding.ASCII.GetString(nameBytes, 0, len).Trim();
        }

        private string ReadSfcdSlotName(int slotIndex)
        {
            int slotOffset = SfcdCharTableBase + (slotIndex * SfcdCharSlotSize);
            int nameOffset = slotOffset + SfcdNameOffset;

            byte[] nameBytes = GetBytesByOffset(nameOffset, SfcdNameLength);

            int zeroIndex = Array.IndexOf(nameBytes, (byte)0);
            int len = zeroIndex >= 0 ? zeroIndex : SfcdNameLength;

            return System.Text.Encoding.ASCII.GetString(nameBytes, 0, len).Trim();
        }

        private string ReadSfcdSlotNameFromStatsBase(int statsBaseOffset)
        {
            // statsBaseOffset is slotStart + 0x10
            int nameOffset = statsBaseOffset - 0x0A; // -> slotStart + 0x06

            byte[] bytes = GetBytesByOffset(nameOffset.ToString("X"), 10);

            // trim at first 0x00
            int end = Array.IndexOf(bytes, (byte)0x00);
            if (end < 0)
            {
                end = bytes.Length;
            }

            var name = System.Text.Encoding.ASCII.GetString(bytes, 0, end).Trim();
            return name;
        }


        private string GetSfcdItemName(byte rawId)
        {
            if (_shiningForceCDItemNamesByRawId == null)
            {
                return null;
            }

            if (rawId == 0xFF || rawId == 0x7F)
            {
                return null;
            }

            bool equipped = (rawId & 0x80) != 0;
            byte baseId = (byte)(rawId & 0x7F);

            if (_shiningForceCDItemNamesByRawId.TryGetValue(baseId, out var baseName))
            {
                return equipped ? baseName + " (Equipped)" : baseName;
            }

            // If some books truly use 0x80..0xFF as non-equipped raw IDs,
            // you can keep a raw fallback:
            if (_shiningForceCDItemNamesByRawId.TryGetValue(rawId, out var rawName))
            {
                return rawName;
            }

            return null;
        }

        private void PopulateShiningForceCDCharacterDetails(ShiningForceCDCharacterItem characterItem)
        {
            //LogError($"[SFCD] Selected: {characterItem.Name} ItemsOffset={string.Join(",", characterItem.ItemsOffset)}");
            //byte[] dump = GetBytesByOffset("D536", 12);
            //LogError("[SFCD] Dump @D536 (12) = " + BitConverter.ToString(dump));

            ResetShiningForceCDControls(false);

            // Always clear listboxes before repopulating
            shiningForceCDItemsListBox.Items.Clear();
            shiningForceCDMagicListBox.Items.Clear();

            string classCode = GetClassCode(characterItem.BaseOffset);
            shiningForceCDCurrentClassTb.Text = classCode;

            string value;
            long val;

            value = GetValueByOffset(characterItem.LevelOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDCurrentLevelTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.AttackBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDAttackBaseTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.AttackEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDAttackEquipTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.DefenseBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDDefenseBaseTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.DefenseEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDDefenseEquipTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.AgilityBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDAgilityBaseTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.AgilityEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDAgilityEquipTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.MoveBaseOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDMoveBaseTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.MoveEquipOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDMoveEquipTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.ExperienceOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDExperienceTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.PresentHPOffset, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDPresentHPTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.MaximumHPOffset, 2);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDMaxHPTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.PresentMPOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDPresentMPTb.Text = val.ToString();

            value = GetValueByOffset(characterItem.MaximumMPOffset, 1);
            val = long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            shiningForceCDMaxMPTb.Text = val.ToString();

            try
            {
                foreach (string itemIdOffset in characterItem.ItemsOffset)
                {
                    byte raw = GetBytesByOffset(itemIdOffset, 1)[0];

                    // adjust empties if needed; DO NOT skip 0x00 if that is Medical Herb for you
                    if (raw == 0xFF || raw == 0x7F)
                    {
                        continue;
                    }

                    string name = GetSfcdItemName(raw);

                    if (name != null)
                    {
                        shiningForceCDItemsListBox.Items.Add(name);
                    }
                    else
                    {
                        shiningForceCDItemsListBox.Items.Add($"Unknown Item (0x{raw:X2})");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message + " Occurred while attempting to load SFCD items.");
            }

            // If listbox contains objects, DisplayMember works; if it contains strings too, it's harmless.
            shiningForceCDItemsListBox.DisplayMember = "Name";

            // ----------------------------
            // MAGIC (still 1 byte each)
            // ----------------------------
            try
            {
                foreach (string magicLoc in characterItem.MagicOffset)
                {
                    value = GetValueByOffset(magicLoc, 1);

                    // Debug logging
                    LogError($"[SFCD] Magic @0x{magicLoc} = 0x{value}");

                    if (value == "3F")
                    {
                        continue;
                    }

                    ShiningForceCDMagicItem magic = GetShiningForceCDMagicItem(value);
                    if (magic != null)
                    {
                        shiningForceCDMagicListBox.Items.Add(magic);
                    }
                    else
                    {
                        shiningForceCDMagicListBox.Items.Add($"Unknown Magic (0x{value})");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message + " Occurred while attempting to load SFCD magic.");
            }

            shiningForceCDMagicListBox.DisplayMember = "Name";
        }

        private void PopulateShiningForceCDItemsList(ShiningForceCDBook currentBook)
        {
            _shiningForceCDItemNamesByRawId = new Dictionary<byte, string>();

            void Add(string name, string hexRawId)
            {
                byte rawId = byte.Parse(hexRawId, System.Globalization.NumberStyles.HexNumber);

                if (_shiningForceCDItemNamesByRawId.TryGetValue(rawId, out var existing) && existing != name)
                {
                    // Keep both so we never silently lie.
                    //_shiningForceCDItemNamesByRawId[rawId] = existing + " / " + name;
                    LogError($"[SFCD] Item ID collision 0x{rawId:X2}: '{existing}' vs '{name}'");
                    return;
                }

                _shiningForceCDItemNamesByRawId[rawId] = name;
            }

            void AddWithBase(string name, string hexRawId)
            {
                byte rawId = byte.Parse(hexRawId, System.Globalization.NumberStyles.HexNumber);
                Add(name, hexRawId);

                // If it looks like an equipped variant, also map the base id
                if ((rawId & 0x80) != 0)
                {
                    byte baseId = (byte)(rawId & 0x7F);
                    _shiningForceCDItemNamesByRawId[baseId] = name;
                }
            }

            // ---------
            // Common (raw IDs)
            // ---------
            Add("Medical Herb", "00");
            Add("Healing Seed", "01");
            Add("Antidote", "02");
            Add("Healing Rain", "03");
            Add("Angel Wing", "04");
            Add("Powerful Wine", "05");
            Add("Protect Milk", "06");
            Add("Quick Chicken", "07");
            Add("Running Pimento", "08");
            Add("Cheerful Bread", "09");

            // Rings (raw IDs)
            AddWithBase("Power Ring", "0A");
            AddWithBase("Protect Ring", "0B");
            AddWithBase("Quick Ring", "0C");
            AddWithBase("Running Ring", "0D");
            AddWithBase("White Ring", "0E");
            AddWithBase("Black Ring", "0F");
            AddWithBase("Evil Ring", "90");

            // Empty markers (raw IDs)
            Add("Empty", "7F");
            AddWithBase("Empty (Equipped?)", "FF");

            // ---------
            // Book 1/2 raw IDs (from your original list)
            // NOTE: these appear to already be raw IDs, not base IDs.
            // ---------
            if (currentBook == ShiningForceCDBook.Book1TowardsTheRootOfEvil || currentBook == ShiningForceCDBook.Book2TheEvilGodAwakes)
            {
                AddWithBase("Leather Glove", "91");
                AddWithBase("Power Glove", "92");
                AddWithBase("Battle Glove", "93");
                AddWithBase("Iron Claw", "94");

                AddWithBase("Short Sword", "9D");
                AddWithBase("Broad Sword", "A1");
                AddWithBase("Critical Sword", "A2");
                AddWithBase("Wooden Stick", "A3");
                AddWithBase("Robin's Arrow", "A4");
                AddWithBase("Dark Sword", "A5");
                AddWithBase("Wood Sword", "26");
                AddWithBase("Sword of Hajya", "A7");
                AddWithBase("Bronze Lance", "A8");
                AddWithBase("Steel Lance", "A9");
                AddWithBase("Chrome Lance", "AA");
                AddWithBase("Evil Lance", "AB");
                AddWithBase("Halberd", "AC");
                AddWithBase("Spear", "AD");
                AddWithBase("Power Spear", "AE");
                AddWithBase("Valkyrie", "AF");
                AddWithBase("Hand Axe", "B0");
                AddWithBase("Middle Axe", "B1");
                AddWithBase("Battle Axe", "B2");
                AddWithBase("Heat Axe", "B3");
                AddWithBase("Axe of Atlas", "B4");
                AddWithBase("Wooden Staff", "B5");
                AddWithBase("Protect Staff", "B6");
                AddWithBase("Holy Staff", "B7");
                AddWithBase("Power Stick", "B8");
                AddWithBase("Demon Rod", "B9");
                AddWithBase("Flail", "BA");
                AddWithBase("Wooden Arrow", "BB");
                AddWithBase("Steel Arrow", "BC");
                AddWithBase("Assault Shell", "BD");
                AddWithBase("Buster Shot", "BE");
                AddWithBase("Short Axe", "97");
                AddWithBase("Bronze Rod", "98"); 
                AddWithBase("Iron Rod", "99"); 
                AddWithBase("Iron Arrow", "9A"); 
                AddWithBase("Club", "15");
                AddWithBase("Club", "95");

                // Proven in your Book1 save:
                AddWithBase("Steel Sword", "A0");
                AddWithBase("Steel Sword", "20"); // keep if you ever see raw 0x20
            }
            else if (currentBook == ShiningForceCDBook.Book3ANewChallenge || currentBook == ShiningForceCDBook.Book4TheLastBattle)
            {
                // ---------
                // Book 3/4 raw IDs (from your list)
                // These are ALSO raw IDs (0x91..0xAF etc), so they WILL collide.
                // That’s OK — collisions get logged and preserved as “A / B”.
                // ---------
                AddWithBase("Misty Knuckle", "91");
                AddWithBase("Giant Knuckle", "92");
                AddWithBase("Large Axe", "93");
                AddWithBase("Earth Axe", "94");
                AddWithBase("Great Rod", "95");
                AddWithBase("Mystery Staff", "96");
                AddWithBase("Hyper Cannon", "97");
                AddWithBase("Shut Cannon", "98");
                AddWithBase("Buster Sword", "99");
                AddWithBase("Counter Sword", "9A");
                AddWithBase("Light Sword", "26");
                AddWithBase("Javelin", "9B");
                AddWithBase("Chrome Lance", "9C");
                AddWithBase("Samurai Sword", "9D");
                AddWithBase("Higins", "9E");
                AddWithBase("Murasame", "9F");
                AddWithBase("Murasana", "A0");
                AddWithBase("Iris Blade", "A1");
                AddWithBase("Kamikaze Axe", "A2");
                AddWithBase("Kizer Knuckle", "A3");
                AddWithBase("Venom Javelin", "A4");
                AddWithBase("Work Glove", "A5");
                AddWithBase("Pegaus Wing", "A7");
                AddWithBase("Mithril", "A8");
                AddWithBase("Steel Sword", "A9");
                AddWithBase("Broad Sword", "AA");
                AddWithBase("Battle Axe", "AB");
                AddWithBase("Axe of Atlas", "AC");
                AddWithBase("Protect Staff", "AD");
                AddWithBase("Chirrup Hummer", "AE");
                AddWithBase("Teddy's Coat", "AF");
            }
        }

        private void PopulateShiningForceCDMagicList()
        {
            shiningForceCDMagicList = new List<ShiningForceCDMagicItem>();

            // A very typical SF-style encoding: base spell id + (levelGroup * 0x40)
            // Where levelGroup: 0 = L1, 1 = L2, 2 = L3, 3 = L4
            // And 0x3F is often used for "Empty".
            shiningForceCDMagicList.AddRange(new[]
            {
                // ----- Level 1 -----
                new ShiningForceCDMagicItem("Heal 1",   "00"),
                new ShiningForceCDMagicItem("Aura 1",   "01"),
                new ShiningForceCDMagicItem("Detox 1",  "02"),
                new ShiningForceCDMagicItem("Boost 1",  "03"),
                new ShiningForceCDMagicItem("Slow 1",   "04"),
                new ShiningForceCDMagicItem("Attack 1", "05"),
                new ShiningForceCDMagicItem("Dispel 1", "06"),
                new ShiningForceCDMagicItem("Muddle 1", "07"),
                new ShiningForceCDMagicItem("Desoul 1", "08"),
                new ShiningForceCDMagicItem("Sleep 1",  "09"),
                new ShiningForceCDMagicItem("Blaze 1",  "0B"),
                new ShiningForceCDMagicItem("Freeze 1", "0C"),
                new ShiningForceCDMagicItem("Bolt 1",   "0D"),
                new ShiningForceCDMagicItem("Hell 1",   "0E"),
                new ShiningForceCDMagicItem("Egress 1", "0A"),
                new ShiningForceCDMagicItem("Empty",    "3F"),

                // ----- Level 2 -----
                new ShiningForceCDMagicItem("Heal 2",   "40"),
                new ShiningForceCDMagicItem("Aura 2",   "41"),
                new ShiningForceCDMagicItem("Detox 2",  "42"),
                new ShiningForceCDMagicItem("Boost 2",  "43"),
                new ShiningForceCDMagicItem("Slow 2",   "44"),
                new ShiningForceCDMagicItem("Attack 2", "45"),
                new ShiningForceCDMagicItem("Dispel 2", "46"),
                new ShiningForceCDMagicItem("Muddle 2", "47"),
                new ShiningForceCDMagicItem("Desoul 2", "48"),
                new ShiningForceCDMagicItem("Sleep 2",  "49"),
                new ShiningForceCDMagicItem("Blaze 2",  "4B"),
                new ShiningForceCDMagicItem("Freeze 2", "4C"),
                new ShiningForceCDMagicItem("Bolt 2",   "4D"),
                new ShiningForceCDMagicItem("Hell 2",   "4E"),
                new ShiningForceCDMagicItem("Egress 2", "4A"),

                // ----- Level 3 -----
                new ShiningForceCDMagicItem("Heal 3",   "80"),
                new ShiningForceCDMagicItem("Aura 3",   "81"),
                new ShiningForceCDMagicItem("Detox 3",  "82"),
                new ShiningForceCDMagicItem("Boost 3",  "83"),
                new ShiningForceCDMagicItem("Slow 3",   "84"),
                new ShiningForceCDMagicItem("Attack 3", "85"),
                new ShiningForceCDMagicItem("Dispel 3", "86"),
                new ShiningForceCDMagicItem("Muddle 3", "87"),
                new ShiningForceCDMagicItem("Desoul 3", "88"),
                new ShiningForceCDMagicItem("Sleep 3",  "89"),
                new ShiningForceCDMagicItem("Blaze 3",  "8B"),
                new ShiningForceCDMagicItem("Freeze 3", "8C"),
                new ShiningForceCDMagicItem("Bolt 3",   "8D"),
                new ShiningForceCDMagicItem("Hell 3",   "8E"),
                new ShiningForceCDMagicItem("Egress 3", "8A"),

                // ----- Level 4 -----
                new ShiningForceCDMagicItem("Heal 4",   "C0"),
                new ShiningForceCDMagicItem("Aura 4",   "C1"),
                new ShiningForceCDMagicItem("Detox 4",  "C2"),
                new ShiningForceCDMagicItem("Boost 4",  "C3"),
                new ShiningForceCDMagicItem("Slow 4",   "C4"),
                new ShiningForceCDMagicItem("Attack 4", "C5"),
                new ShiningForceCDMagicItem("Dispel 4", "C6"),
                new ShiningForceCDMagicItem("Muddle 4", "C7"),
                new ShiningForceCDMagicItem("Desoul 4", "C8"),
                new ShiningForceCDMagicItem("Sleep 4",  "C9"),
                new ShiningForceCDMagicItem("Blaze 4",  "CB"),
                new ShiningForceCDMagicItem("Freeze 4", "CC"),
                new ShiningForceCDMagicItem("Bolt 4",   "CD"),
                new ShiningForceCDMagicItem("Hell 4",   "CE"),
                new ShiningForceCDMagicItem("Egress 4", "CA"),
            });
        }


        private void LogError(string errMsg)
        {
            // Never let logging throw — it is called from catch blocks, so a failure
            // here (e.g. a read-only install directory) must not crash the app.
            try
            {
                string filePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                filePath += @"\errorlog.txt";

                using (TextWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(errMsg + " Added: " + DateTime.Now.ToString());
                }
            }
            catch
            {
                // Swallow: logging is best-effort.
            }
        }

        private void ClearErrorLog()
        {
            // Create the file path
            string filePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            filePath += @"\errorlog.txt";

            string errMessage = string.Empty;

            // Delete the file
            try
            {
                File.Delete(filePath);
                MessageBox.Show("The error log has been cleared.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException ioE)
            {
                errMessage = ioE.Message + " Occurred during call to ClearErroLog().";
                MessageBox.Show(errMessage, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException uaE)
            {
                errMessage = uaE.Message + " Occurred during call to ClearErroLog().";
                MessageBox.Show(errMessage, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                errMessage = e.Message + " Occurred during call to ClearErroLog().";
                MessageBox.Show(errMessage, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetSfcdCaches()
        {
            _sfcdClassTableLoadedFromPath = null;
            _sfcdClassTable = null;
        }

        /// <summary>
        /// Validates that the chosen file is a Kega Fusion save state ("GST" header) of
        /// the size expected for the selected game's console. This prevents editing the
        /// wrong file at hardcoded offsets, which would silently corrupt a save.
        /// </summary>
        private bool ValidateSaveStateFile(string path, out string error)
        {
            error = null;

            FileInfo info;
            try
            {
                info = new FileInfo(path);
            }
            catch (Exception ex)
            {
                error = "The selected file could not be accessed: " + ex.Message;
                return false;
            }

            if (!info.Exists)
            {
                error = "The selected file no longer exists.";
                return false;
            }

            bool isSegaCd = ActivePanel == AppPanel.ShiningForceCD;
            long expectedSize = isSegaCd ? GST_SIZE_SEGA_CD : GST_SIZE_GENESIS;
            string console = isSegaCd ? "Sega CD" : "Genesis";

            byte[] header = new byte[GST_MAGIC.Length];
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Read(header, 0, header.Length);
                }
            }
            catch (Exception ex)
            {
                error = "The selected file could not be read: " + ex.Message;
                return false;
            }

            if (!header.SequenceEqual(GST_MAGIC))
            {
                error = "This is not a Kega Fusion save state (its header is missing the \"GST\" signature).";
                return false;
            }

            if (info.Length != expectedSize)
            {
                error = $"This does not look like a {console} save state for {GetSelectedGameTitle()}." +
                        $"{Environment.NewLine}{Environment.NewLine}" +
                        $"Expected size: {expectedSize:N0} bytes{Environment.NewLine}" +
                        $"Selected file: {info.Length:N0} bytes{Environment.NewLine}{Environment.NewLine}" +
                        "Make sure you picked the correct save state for this game.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Copies the save state to "&lt;path&gt;.bak" once per session before the first
        /// edit, so the pre-edit state can always be recovered.
        /// </summary>
        private void EnsureBackup(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path) || _backedUpThisSession.Contains(path))
                {
                    return;
                }

                File.Copy(path, path + ".bak", true);
                _backedUpThisSession.Add(path);
            }
            catch (Exception ex)
            {
                LogError(ex.Message + " Occurred while creating a backup (.bak) of the save state file.");
            }
        }

        // ── In-memory save-state buffer ──────────────────────────────────────
        // The whole file is loaded once into _fileBytes; all reads and writes go
        // through it, and each "Update Save State" flushes it to disk in a single
        // write (instead of re-opening the file for every field).
        private byte[] _fileBytes;
        private string _loadedPath;

        private static int ParseOffset(string offset) =>
            int.Parse(offset, System.Globalization.NumberStyles.HexNumber);

        /// <summary>Ensures _fileBytes holds the contents of the file named in the path box.</summary>
        private bool EnsureBufferLoaded()
        {
            string path = saveStateFileTb.Text;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            if (_fileBytes != null && string.Equals(_loadedPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                _fileBytes = File.ReadAllBytes(path);
                _loadedPath = path;
                return true;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while loading the save state into memory.");
                _fileBytes = null;
                _loadedPath = null;
                return false;
            }
        }

        /// <summary>Writes the in-memory buffer back to disk in a single operation.</summary>
        private bool SaveBufferToDisk()
        {
            if (_fileBytes == null || string.IsNullOrEmpty(_loadedPath))
            {
                return false;
            }

            try
            {
                File.WriteAllBytes(_loadedPath, _fileBytes);
                return true;
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred while saving the save state to disk.");
                MessageBox.Show("The save state could not be written to disk. See the error log for details.",
                    "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>Copies <paramref name="count"/> bytes from source[index..] into the buffer at offset.</summary>
        private bool WriteBytes(int offset, byte[] source, int index, int count)
        {
            if (!EnsureBufferLoaded())
            {
                return false;
            }

            if (offset < 0 || count < 0 || index < 0
                || offset + count > _fileBytes.Length || index + count > source.Length)
            {
                LogError($"Refused out-of-range write at offset 0x{offset:X} ({count} bytes).");
                return false;
            }

            Buffer.BlockCopy(source, index, _fileBytes, offset, count);
            return true;
        }

        private string GetValueByOffset(string offset, int bytesToRead)
        {
            if (!EnsureBufferLoaded())
            {
                return string.Empty;
            }

            try
            {
                return BitConverter.ToString(_fileBytes, ParseOffset(offset), bytesToRead).Replace("-", null);
            }
            catch (Exception e)
            {
                LogError(e.Message + " Occurred when attempting to read a value by its offset.");
                return string.Empty;
            }
        }

        private byte[] GetBytesByOffset(string offset, int bytesToRead) =>
            GetBytesByOffset(ParseOffset(offset), bytesToRead);

        private byte[] GetBytesByOffset(int offset, int length)
        {
            if (!EnsureBufferLoaded() || offset < 0 || offset >= _fileBytes.Length)
            {
                return Array.Empty<byte>();
            }

            int available = Math.Min(length, _fileBytes.Length - offset);
            byte[] buffer = new byte[available];
            Buffer.BlockCopy(_fileBytes, offset, buffer, 0, available);
            return buffer;
        }


        // Genesis/Sega CD saves are big-endian, so multi-byte values are reversed
        // before writing. Each overload builds its bytes and routes through WriteBytes.
        private bool SetValueByOffset(string value, string offset)
        {
            byte[] bytes = BitConverter.GetBytes(Convert.ToInt32(value)).Reverse().ToArray();
            return WriteBytes(ParseOffset(offset), bytes, 0, 4);
        }

        private bool SetValueByOffset(long value, string offset)
        {
            byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
            return WriteBytes(ParseOffset(offset), bytes, 0, 8);
        }

        private bool SetValueByOffset(int value, string offset, int numBytes = 0)
        {
            byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
            return numBytes == 0
                ? WriteBytes(ParseOffset(offset), bytes, 0, 4)
                : WriteBytes(ParseOffset(offset), bytes, 1, numBytes);
        }

        private bool SetValueByOffset(short value, string offset, int index = 0, int numBytes = 0)
        {
            byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
            return (numBytes == 0 && index == 0)
                ? WriteBytes(ParseOffset(offset), bytes, 0, 2)
                : WriteBytes(ParseOffset(offset), bytes, index, numBytes);
        }

        private bool SetValueByOffset(ushort value, string offset, int index = 0, int numBytes = 0)
        {
            byte[] bytes = BitConverter.GetBytes(value).Reverse().ToArray();
            return numBytes == 0
                ? WriteBytes(ParseOffset(offset), bytes, 0, 2)
                : WriteBytes(ParseOffset(offset), bytes, index, numBytes);
        }

        private bool SetUInt32BigEndianByOffset(uint value, string offset)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);   // save is big-endian
            }
            return WriteBytes(ParseOffset(offset), bytes, 0, 4);
        }

        private bool SetByteByOffset(byte value, string offset) =>
            WriteBytes(ParseOffset(offset), new[] { value }, 0, 1);

        private bool SetValueByOffset(byte value, string offset) =>
            WriteBytes(ParseOffset(offset), new[] { value }, 0, 1);

        private void UpdateShiningSaveState()
        {
            if (!EnsureBufferLoaded())
            {
                return;   // no valid save state loaded
            }

            EnsureBackup(saveStateFileTb.Text);
            ShiningCharacterItem charItem = shiningCharacterCmb.SelectedItem as ShiningCharacterItem;
            if (TryReadField(shiningNewGoldTb, "Gold", 0, 4294967295, out long gold1))
            {
                SetValueByOffset((int)gold1, SHINING_GOLD_LOC);
            }

            if (TryReadField(shiningNewExpTb, "Experience", 0, 4294967295, out long exp2))
            {
                SetValueByOffset((int)exp2, charItem.ExpLoc);
            }

            if (TryReadField(shiningNewCurHPTb, "Current HP", 0, 65535, out long hp3))
            {
                SetValueByOffset((short)hp3, charItem.CurHPLoc);
            }

            if (TryReadField(shiningNewMaxHPTb, "Max HP", 0, 65535, out long hp4))
            {
                SetValueByOffset((short)hp4, charItem.MaxHPLoc);
            }

            if (TryReadField(shiningNewCurMPTb, "Current MP", 0, 65535, out long tp5))
            {
                SetValueByOffset((short)tp5, charItem.CurMPLoc);
            }

            if (TryReadField(shiningNewMaxMPTb, "Max MP", 0, 65535, out long tp6))
            {
                SetValueByOffset((short)tp6, charItem.MaxMPLoc);
            }

            if (TryReadField(shiningNewIQTb, "IQ", 0, 65535, out long str7))
            {
                SetValueByOffset((short)str7, charItem.IQLoc);
            }

            if (TryReadField(shiningNewLuckTb, "Luck", 0, 65535, out long str8))
            {
                SetValueByOffset((short)str8, charItem.LuckLoc);
            }

            if (TryReadField(shiningNewAttackTb, "Attack", 0, 65535, out long str9))
            {
                SetValueByOffset((short)str9, charItem.AttackLoc);
            }

            if (!SaveBufferToDisk())
            {
                return;   // write failed; SaveBufferToDisk already reported it
            }

            MessageBox.Show("The save state update process has completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetShiningControls(false, true);
            PopulateShiningCurrentGold();
            PopulateShiningCharacterDetails(charItem);
        }

        private ShiningForceItem GetShiningForceItem(string id)
        {
            return shiningForceItemsList.FirstOrDefault(i => i.ID.ToUpper() == id.ToUpper());
        }

        private ShiningForceMagicItem GetShiningForceMagicItem(string id)
        {
            return shiningForceMagicList.FirstOrDefault(m => m.ID.ToUpper() == id.ToUpper());
        }

        private void UpdateShiningForceSaveState()
        {
            if (!EnsureBufferLoaded())
            {
                return;   // no valid save state loaded
            }

            EnsureBackup(saveStateFileTb.Text);
            ShiningForceCharacterItem charItem = shiningForceCharacterCmb.SelectedItem as ShiningForceCharacterItem;
            if (TryReadField(shiningForceNewGoldTb, "Gold", 0, 16777215, out long gold10))
            {
                SetValueByOffset((int)gold10, SHINING_FORCE_GOLD_LOC, 3);
            }

            if (charItem != null)
            {
                if (TryReadField(shiningForceNewAttackTb, "Attack", 0, 255, out long attack11))
                {
                    SetValueByOffset((short)attack11, charItem.AttackLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewDefenseTb, "Defense", 0, 255, out long defense12))
                {
                    SetValueByOffset((short)defense12, charItem.DefenseLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewAgilityTb, "Agility", 0, 255, out long agility13))
                {
                    SetValueByOffset((short)agility13, charItem.AgilityLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewMoveTb, "Move", 0, 255, out long move14))
                {
                    SetValueByOffset((short)move14, charItem.MoveLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewExpTb, "Experience", 0, 255, out long exp15))
                {
                    SetValueByOffset((short)exp15, charItem.ExperienceLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewCurHPTb, "Current HP", 0, 255, out long hp16))
                {
                    SetValueByOffset((short)hp16, charItem.CurrentHPLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewMaxHPTb, "Max HP", 0, 255, out long maxHP17))
                {
                    SetValueByOffset((short)maxHP17, charItem.MaxHPLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewCurMPTb, "Current MP", 0, 255, out long mp18))
                {
                    SetValueByOffset((short)mp18, charItem.CurrentMPLoc, 1, 1);
                }

                if (TryReadField(shiningForceNewMaxMPTb, "Max MP", 0, 255, out long maxMP19))
                {
                    SetValueByOffset((short)maxMP19, charItem.MaxMPLoc, 1, 1);
                }
            }

            if (!SaveBufferToDisk())
            {
                return;   // write failed; SaveBufferToDisk already reported it
            }

            MessageBox.Show("The save state update process has completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetShiningForceControls(false);
            PopulateShiningForceCurrentGold();

            if (charItem != null)
            {
                PopulateShiningForceCharacterDetails(charItem);
            }
        }

        private ShiningForce2Item GetShiningForce2Item(string id)
        {
            return shiningForce2ItemsList.FirstOrDefault(i => i.ID.ToUpper() == id.ToUpper());
        }

        private ShiningForce2MagicItem GetShiningForce2MagicItem(string id)
        {
            return shiningForce2MagicList.FirstOrDefault(m => m.ID.ToUpper() == id.ToUpper());
        }

        private void UpdateShiningForce2SaveState()
        {
            if (!EnsureBufferLoaded())
            {
                return;   // no valid save state loaded
            }

            EnsureBackup(saveStateFileTb.Text);
            ShiningForce2CharacterItem charItem = shiningForce2CharacterCmb.SelectedItem as ShiningForce2CharacterItem;
            if (TryReadField(shiningForce2NewGoldTb, "Gold", 0, 65535, out long gold20))
            {
                SetValueByOffset((ushort)gold20, SHINING_FORCE_2_GOLD_LOC, 0, 2);
            }

            if (charItem != null)
            {
                if (TryReadField(shiningForce2NewAttackBaseTb, "Attack base", 0, 255, out long attack21))
                {
                    SetValueByOffset((short)attack21, charItem.AttackBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewAttackEquipTb, "Attack equip", 0, 255, out long attack22))
                {
                    SetValueByOffset((short)attack22, charItem.AttackEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewDefenseBaseTb, "Defense base", 0, 255, out long defense23))
                {
                    SetValueByOffset((short)defense23, charItem.DefenseBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewDefenseEquipTb, "Defense equip", 0, 255, out long defense24))
                {
                    SetValueByOffset((short)defense24, charItem.DefenseEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewAgilityBaseTb, "Agility base", 0, 255, out long agility25))
                {
                    SetValueByOffset((short)agility25, charItem.AgilityBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewAgilityEquipTb, "Agility equip", 0, 255, out long agility26))
                {
                    SetValueByOffset((short)agility26, charItem.AgilityEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewMoveBaseTb, "Move base", 0, 255, out long move27))
                {
                    SetValueByOffset((short)move27, charItem.MoveBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewMoveEquipTb, "Move equip", 0, 255, out long move28))
                {
                    SetValueByOffset((short)move28, charItem.MoveEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewExpTb, "Experience", 0, 255, out long exp29))
                {
                    SetValueByOffset((short)exp29, charItem.ExperienceOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewPresentHPTb, "Present HP", 0, 65535, out long hp30))
                {
                    SetValueByOffset((short)hp30, charItem.PresentHPOffset, 0, 0);
                }

                if (TryReadField(shiningForce2NewMaxHPTb, "Max HP", 0, 65535, out long hp31))
                {
                    SetValueByOffset((short)hp31, charItem.MaximumHPOffset, 0, 0);
                }

                if (TryReadField(shiningForce2NewPresentMPTb, "Present MP", 0, 255, out long mp32))
                {
                    SetValueByOffset((short)mp32, charItem.PresentMPOffset, 1, 1);
                }

                if (TryReadField(shiningForce2NewMaxMPTb, "Pmax MP", 0, 255, out long mp33))
                {
                    SetValueByOffset((short)mp33, charItem.MaximumMPOffset, 1, 1);
                }
            }

            if (!SaveBufferToDisk())
            {
                return;   // write failed; SaveBufferToDisk already reported it
            }

            MessageBox.Show("The save state update process has completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetShiningForce2Controls(false);
            PopulateShiningForce2CurrentGold();

            if (charItem != null)
            {
                PopulateShiningForce2CharacterDetails(charItem);
            }
        }

        // Returns 1..4, or 0 if unknown
        private int GetShiningForceCDBookNumber()
        {
            byte raw = GetBytesByOffset(SHINING_FORCE_CD_BOOK_INDEX_OFFSET, 1)[0];
            LogError($"[SFCD] Book byte @0x{SHINING_FORCE_CD_BOOK_INDEX_OFFSET} = 0x{raw:X2} ({raw})");

            return raw is >= 1 and <= 4 ? raw : 0;
        }

        private void DetermineShiningForceCDCurrentBook()
        {
            int bookNum = GetShiningForceCDBookNumber();

            _shiningForceCDBook = bookNum switch
            {
                1 => ShiningForceCDBook.Book1TowardsTheRootOfEvil,
                2 => ShiningForceCDBook.Book2TheEvilGodAwakes,
                3 => ShiningForceCDBook.Book3ANewChallenge,
                4 => ShiningForceCDBook.Book4TheLastBattle,
                _ => ShiningForceCDBook.UnknownBook
            };
        }

        private string GetShiningForceCDCurrentBookString()
        {
            switch (_shiningForceCDBook)
            {
                case ShiningForceCDBook.Book1TowardsTheRootOfEvil:
                    return "Book 1: Towards the Root of Evil";
                case ShiningForceCDBook.Book2TheEvilGodAwakes:
                    return "Book 2: The Evil God Awakes";
                case ShiningForceCDBook.Book3ANewChallenge:
                    return "Book 3: A New Challenge";
                case ShiningForceCDBook.Book4TheLastBattle:
                    return "Book 4: The Last Battle";
                default:
                    return "Unknown Book";
            }
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

        /// <summary>
        /// Returns SDMN, WARR, PLDN, WIZ, VICR, etc for the character record at <paramref name="characterBaseOffset"/>.
        /// </summary>
        private string GetClassCode(int characterBaseOffset)
        {
            EnsureSfcdClassTableLoaded();

            byte classId = GetBytesByOffset(characterBaseOffset + SfcdClassIdOffset, 1)[0];

            if (_sfcdClassTable == null || _sfcdClassTable.Count == 0)
            {
                return "????";
            }

            if (classId >= _sfcdClassTable.Count)
            {
                return $"ID:{classId}";
            }

            return _sfcdClassTable[classId];
        }


        private void EnsureSfcdClassTableLoaded()
        {
            string path = saveStateFileTb.Text;

            // Skip only if we already have a valid table for THIS file
            if (_sfcdClassTable != null
                && _sfcdClassTable.Count > 0
                && _sfcdClassTableLoadedFromPath != null
                && string.Equals(_sfcdClassTableLoadedFromPath, path, StringComparison.OrdinalIgnoreCase)
                && _sfcdClassTable.Contains("SDMN"))
            {
                return;
            }

            _sfcdClassTableLoadedFromPath = path;

            // Read from the in-memory buffer (loaded once).
            if (!EnsureBufferLoaded())
            {
                return;
            }
            byte[] all = _fileBytes;

            // Find the signature: 04 SDMN 04 HERO 04 KNTE 04 PLDN
            int start = FindClassTableOffsetBySignature(all);
            if (start < 0)
            {
                _sfcdClassTable = new List<string>(); // stays empty => "????"
                return;
            }

            _sfcdClassTable = ReadLenPrefixedStringTableFromBytes(all, start, maxItems: 200);

            LogError($"[SFCD] Class table loaded: {_sfcdClassTable.Count} entries. First={_sfcdClassTable[0]}");
        }

        private static List<string> ReadLenPrefixedStringTableFromBytes(byte[] all, int startOffset, int maxItems)
        {
            var list = new List<string>(maxItems);
            int offset = startOffset;

            for (int i = 0; i < maxItems; i++)
            {
                if (offset < 0 || offset >= all.Length)
                    break;

                byte len = all[offset];

                // class codes are short; table also includes other strings, but still usually small
                if (len == 0 || len > 20)
                    break;

                if (offset + 1 + len > all.Length)
                    break;

                string s = Encoding.ASCII.GetString(all, offset + 1, len).TrimEnd('\0', ' ');

                if (string.IsNullOrWhiteSpace(s))
                    break;

                list.Add(s);
                offset += 1 + len;
            }

            return list;
        }

        /// <summary>
        /// Scans the file for: 04 SDMN 04 HERO 04 KNTE 04 PLDN
        /// and returns the offset of the first length byte (0x04) if found.
        /// </summary>
        private static int FindClassTableOffsetBySignature(byte[] all)
        {
            byte[] sig =
            {
                0x04, (byte)'S', (byte)'D', (byte)'M', (byte)'N',
                0x04, (byte)'H', (byte)'E', (byte)'R', (byte)'O',
                0x04, (byte)'K', (byte)'N', (byte)'T', (byte)'E',
                0x04, (byte)'P', (byte)'L', (byte)'D', (byte)'N',
            };

            for (int i = 0; i <= all.Length - sig.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < sig.Length; j++)
                {
                    if (all[i + j] != sig[j]) { match = false; break; }
                }

                if (match)
                    return i;
            }

            return -1;
        }

        private void UpdateShiningForceCDSaveState()
        {
            if (!EnsureBufferLoaded())
            {
                return;   // no valid save state loaded
            }

            EnsureBackup(saveStateFileTb.Text);
            ShiningForceCDCharacterItem charItem = shiningForceCDSelectCharacterCmb.SelectedItem as ShiningForceCDCharacterItem;

            if (TryReadField(shiningForceCDNewGoldTb, "Gold", 0, 4294967295, out long newGold34))
            {
                SetUInt32BigEndianByOffset((uint)newGold34, SHINING_FORCE_CD_GOLD_LOC);
            }

            if (charItem != null)
            {
                if (TryReadField(shiningForceCDNewAttackBaseTb, "Attack base", 0, 255, out long attack35))
                {
                    SetValueByOffset((short)attack35, charItem.AttackBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewAttackEquipTb, "Attack equip", 0, 255, out long attack36))
                {
                    SetValueByOffset((short)attack36, charItem.AttackEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewDefenseBaseTb, "Defense base", 0, 255, out long defense37))
                {
                    SetValueByOffset((short)defense37, charItem.DefenseBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewDefenseEquipTb, "Defense equip", 0, 255, out long defense38))
                {
                    SetValueByOffset((short)defense38, charItem.DefenseEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewAgilityBaseTb, "Agility base", 0, 255, out long agility39))
                {
                    SetValueByOffset((short)agility39, charItem.AgilityBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewAgilityEquipTb, "Agility equip", 0, 255, out long agility40))
                {
                    SetValueByOffset((short)agility40, charItem.AgilityEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewMoveBaseTb, "Move base", 0, 255, out long move41))
                {
                    SetValueByOffset((short)move41, charItem.MoveBaseOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewMoveEquipTb, "Move equip", 0, 255, out long move42))
                {
                    SetValueByOffset((short)move42, charItem.MoveEquipOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewExperienceTb, "Experience", 0, 255, out long exp43))
                {
                    SetValueByOffset((short)exp43, charItem.ExperienceOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewPresentHPTb, "Present HP", 0, 65535, out long hp44))
                {
                    SetValueByOffset((short)hp44, charItem.PresentHPOffset, 0, 0);
                }

                if (TryReadField(shiningForceCDNewMaxHPTb, "Max HP", 0, 65535, out long hp45))
                {
                    SetValueByOffset((short)hp45, charItem.MaximumHPOffset, 0, 0);
                }

                if (TryReadField(shiningForceCDNewPresentMPTb, "Present MP", 0, 255, out long mp46))
                {
                    SetValueByOffset((short)mp46, charItem.PresentMPOffset, 1, 1);
                }

                if (TryReadField(shiningForceCDNewMaxMPTb, "Max MP", 0, 255, out long mp47))
                {
                    SetValueByOffset((short)mp47, charItem.MaximumMPOffset, 1, 1);
                }
            }

            if (!SaveBufferToDisk())
            {
                return;   // write failed; SaveBufferToDisk already reported it
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

        private sealed class CharComboItem
        {
            public string Name { get;  set; } = "";
            public int Offset { get; set; }
            public string SlotId { get; set; } = "";
        }
        #endregion
    }
}