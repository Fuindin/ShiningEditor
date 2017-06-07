namespace ShiningEditor
{
    partial class ErrorLogView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.outputTb = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // outputTb
            // 
            this.outputTb.BackColor = System.Drawing.Color.White;
            this.outputTb.Location = new System.Drawing.Point(13, 13);
            this.outputTb.Multiline = true;
            this.outputTb.Name = "outputTb";
            this.outputTb.ReadOnly = true;
            this.outputTb.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.outputTb.Size = new System.Drawing.Size(735, 600);
            this.outputTb.TabIndex = 0;
            // 
            // ErrorLogView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(760, 625);
            this.Controls.Add(this.outputTb);
            this.Name = "ErrorLogView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Error Log View";
            this.Load += new System.EventHandler(this.ErrorLogView_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox outputTb;
    }
}