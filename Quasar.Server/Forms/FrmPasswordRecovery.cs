using System;
using System.Windows.Forms;

namespace Quasar.Server.Forms
{
    public partial class FrmPasswordRecovery : Form
    {
        public FrmPasswordRecovery()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmPasswordRecovery));
            this.SuspendLayout();
            // 
            // FrmPasswordRecovery
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 311);
            this.Name = "FrmPasswordRecovery";
            this.Text = "Password Recovery";
            this.ResumeLayout(false);
        }

        // Basic implementation of password recovery
        public void RecoverPassword(string username)
        {
            MessageBox.Show("Password recovery functionality is under development.", "Password Recovery", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
