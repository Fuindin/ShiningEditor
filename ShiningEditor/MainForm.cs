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
                    saveStateFileTb.Text = openFD.FileName;
                    FileLoaded = true;
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
                    ResetShiningControls(false);
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
            byte[] bytes = GetBytesByOffset(SHINING_FORCE_2_GOLD_LOC, 4);

            // Save states store gold as BIG-endian
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            uint gold = BitConverter.ToUInt32(bytes, 0);
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
            shiningForce2NewDefenseBaseTb.Text = "";
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
            // Create a write and open the file
            string filePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            filePath += @"\errorlog.txt";
            TextWriter writer = new StreamWriter(filePath, true);

            // Write the error message to the error log
            writer.WriteLine(errMsg + " Added: " + DateTime.Now.ToString());

            // Close the stream
            writer.Close();
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
            ShiningForce2CharacterItem charItem = shiningForce2CharacterCmb.SelectedItem as ShiningForce2CharacterItem;
            if (shiningForce2NewGoldTb.Text != string.Empty)
            {
                uint gold = 0;
                if (uint.TryParse(shiningForce2NewGoldTb.Text, out gold))
                {
                    SetUInt32BigEndianByOffset(gold, SHINING_FORCE_2_GOLD_LOC);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new gold value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            // Read file once (it's ~1.1MB; totally fine)
            byte[] all = File.ReadAllBytes(path);

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

        private sealed class CharComboItem
        {
            public string Name { get;  set; } = "";
            public int Offset { get; set; }
            public string SlotId { get; set; } = "";
        }
        #endregion
    }
}