using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

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
            None
        }
        #endregion

        #region - Class Fields -
        private bool fileLoaded;
        private const string SHINING_GOLD_LOC = "3B1C";
        private const string SHINING_FORCE_GOLD_LOC = "C107";
        private AppPanel activePanel;
        private List<ShiningForceItem> shiningForceItemsList;
        private List<ShiningForceMagicItem> shiningForceMagicList;
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
                openFD.Title = "Select a " + game + " save state file";
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
                    }
                }
                else
                {
                    saveStateFileTb.Text = string.Empty;
                }
            }
            else
            {
                MessageBox.Show("You must first select a Phantasy Star game from the menu so the correct game data is loaded.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void shiningCharacterCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (shiningCharacterCmb.SelectedIndex >= 0)
            {
                if (FileLoaded)
                {
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

        private void updSaveStateBtn_Click(object sender, EventArgs e)
        {
            UpdateShiningSaveState();
        }

        private void updShiningForceSaveStateBtn_Click(object sender, EventArgs e)
        {
            UpdateShiningForceSaveState();
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
                    break;

                case AppPanel.ShiningInTheDarkness:
                    shiningPanel.Visible = show;
                    if (show)
                    {
                        shiningForcePanel.Visible = !show;
                    }
                    break;

                case AppPanel.ShiningForce:
                    shiningForcePanel.Visible = show;
                    if (show)
                    {
                        shiningPanel.Visible = !show;
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

        private bool SetValueByOffset(short value, string offset, int numBytes = 0)
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

        private bool SetValueByOffset(ushort value, string offset)
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

            if (shiningForceNewAttackTb.Text != string.Empty)
            {
                short attack = 0;
                if (short.TryParse(shiningForceNewAttackTb.Text, out attack))
                {
                    SetValueByOffset(attack, charItem.AttackLoc, 1);
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
                    SetValueByOffset(defense, charItem.DefenseLoc, 1);
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
                    SetValueByOffset(agility, charItem.AgilityLoc, 1);
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
                    SetValueByOffset(move, charItem.MoveLoc, 1);
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
                    SetValueByOffset(exp, charItem.ExperienceLoc, 1);
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
                    SetValueByOffset(hp, charItem.CurrentHPLoc, 1);
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
                    SetValueByOffset(maxHP, charItem.MaxHPLoc, 1);
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
                    SetValueByOffset(mp, charItem.CurrentMPLoc, 1);
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
                    SetValueByOffset(maxMP, charItem.MaxMPLoc, 1);
                }
                else
                {
                    MessageBox.Show("You must enter a numeric value for the new max MP value.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            MessageBox.Show("The save state update process has completed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetShiningForceControls(false);
            PopulateShiningForceCurrentGold();
            PopulateShiningForceCharacterDetails(charItem);
        }
        #endregion
    }
}
