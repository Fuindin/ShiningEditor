using System;
using System.Windows.Forms;

namespace ShiningEditor
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            Theme.ApplyDialog(this);
        }
    }
}
