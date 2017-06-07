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
    public partial class ErrorLogView : Form
    {
        public ErrorLogView()
        {
            InitializeComponent();
        }

        private void ErrorLogView_Load(object sender, EventArgs e)
        {
            outputTb.Text = ReadErrorLog();
        }

        private string ReadErrorLog()
        {
            string logText = string.Empty;

            // Create error log path
            string filePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            filePath += @"\errorlog.txt";

            // Create reader object
            TextReader reader = null;

            // Make sure file exists
            if (File.Exists(filePath))
            {
                try
                {
                    // Open file
                    reader = new StreamReader(filePath);

                    // Read the log info
                    if (reader != null)
                        logText = reader.ReadToEnd();
                }
                catch (IOException ioE)
                {
                    outputTb.Text = ioE.Message + " Occurred during call to ReadErrorLog().";
                }
                catch (OutOfMemoryException oomE)
                {
                    outputTb.Text = oomE.Message + " Occurred during call to ReadErrorLog().";
                }
                catch (NullReferenceException nE)
                {
                    outputTb.Text = nE.Message + " Occurred during call to ReadErrorLog().";
                }
                catch (Exception e)
                {
                    outputTb.Text = e.Message + " Occurred during call to ReadErrorLog().";
                }
                finally
                {
                    // Close the stream
                    reader.Close();
                }
            }

            return logText;
        }
    }
}
