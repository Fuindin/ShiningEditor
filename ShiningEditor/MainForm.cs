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
            Shining,
            None
        }
        #endregion

        #region - Class Fields -
        private bool fileLoaded;
        private const string SHINING_GOLD_LOC = "3B1C";
        private AppPanel activePanel;
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
            ActivePanel = AppPanel.Shining;
            saveStateFileTb.Text = string.Empty;
            ResetShiningControls(true);
            PopulateShiningCharacterList();
            ShowPanel(AppPanel.Shining, true);
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
                        case AppPanel.Shining:
                            PopulateShiningCurrentGold();
                            break;

                            // Add cases here
                    }
                }
                else
                {
                    saveStateFileTb.Text = string.Empty;
                }
            }
            else
            {
                MessageBox.Show("You must first select a Shining game from the menu so the correct game data is loaded.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void updSaveStateBtn_Click(object sender, EventArgs e)
        {
            UpdateShiningSaveState();
        }
        #endregion

        #region - Private Methods -
        private void ShowPanel(AppPanel panel, bool show)
        {
            switch (panel)
            {
                case AppPanel.All:
                    shiningPanel.Visible = show;
                    break;

                case AppPanel.Shining:
                    shiningPanel.Visible = show;
                    if (show)
                    {
                        // Add here
                    }
                    break;

                // Add other cases here
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
                case AppPanel.Shining:
                    game = "Shining in the Darkness";
                    break;

                // Add cases here
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

        private bool SetValueByOffset(int value, string offset)
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

        private bool SetValueByOffset(short value, string offset)
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
                int meseta = 0;
                if (int.TryParse(shiningNewGoldTb.Text, out meseta))
                {
                    SetValueByOffset(meseta, SHINING_GOLD_LOC);
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
        #endregion
    }
}
