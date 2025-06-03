namespace Quasar.Server.Forms
{
    partial class FrmRelaySettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRelaySettings));
            this.groupBoxConnection = new System.Windows.Forms.GroupBox();
            this.txtRelayPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtRelayDeviceId = new System.Windows.Forms.TextBox();
            this.lblDeviceId = new System.Windows.Forms.Label();
            this.txtRelayServerUrl = new System.Windows.Forms.TextBox();
            this.lblRelayServer = new System.Windows.Forms.Label();
            this.groupBoxOptions = new System.Windows.Forms.GroupBox();
            this.chkAutoStartRelay = new System.Windows.Forms.CheckBox();
            this.groupBoxStatus = new System.Windows.Forms.GroupBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBoxConnection.SuspendLayout();
            this.groupBoxOptions.SuspendLayout();
            this.groupBoxStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxConnection
            // 
            this.groupBoxConnection.Controls.Add(this.txtRelayPassword);
            this.groupBoxConnection.Controls.Add(this.lblPassword);
            this.groupBoxConnection.Controls.Add(this.txtRelayDeviceId);
            this.groupBoxConnection.Controls.Add(this.lblDeviceId);
            this.groupBoxConnection.Controls.Add(this.txtRelayServerUrl);
            this.groupBoxConnection.Controls.Add(this.lblRelayServer);
            this.groupBoxConnection.Location = new System.Drawing.Point(12, 12);
            this.groupBoxConnection.Name = "groupBoxConnection";
            this.groupBoxConnection.Size = new System.Drawing.Size(400, 120);
            this.groupBoxConnection.TabIndex = 0;
            this.groupBoxConnection.TabStop = false;
            this.groupBoxConnection.Text = "Relay Connection";
            // 
            // txtRelayPassword
            // 
            this.txtRelayPassword.Location = new System.Drawing.Point(120, 85);
            this.txtRelayPassword.Name = "txtRelayPassword";
            this.txtRelayPassword.PasswordChar = '*';
            this.txtRelayPassword.Size = new System.Drawing.Size(260, 20);
            this.txtRelayPassword.TabIndex = 5;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(15, 88);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(56, 13);
            this.lblPassword.TabIndex = 4;
            this.lblPassword.Text = "Password:";
            // 
            // txtRelayDeviceId
            // 
            this.txtRelayDeviceId.Location = new System.Drawing.Point(120, 55);
            this.txtRelayDeviceId.Name = "txtRelayDeviceId";
            this.txtRelayDeviceId.Size = new System.Drawing.Size(260, 20);
            this.txtRelayDeviceId.TabIndex = 3;
            // 
            // lblDeviceId
            // 
            this.lblDeviceId.AutoSize = true;
            this.lblDeviceId.Location = new System.Drawing.Point(15, 58);
            this.lblDeviceId.Name = "lblDeviceId";
            this.lblDeviceId.Size = new System.Drawing.Size(99, 13);
            this.lblDeviceId.TabIndex = 2;
            this.lblDeviceId.Text = "Device ID (optional):";
            // 
            // txtRelayServerUrl
            // 
            this.txtRelayServerUrl.Location = new System.Drawing.Point(120, 25);
            this.txtRelayServerUrl.Name = "txtRelayServerUrl";
            this.txtRelayServerUrl.Size = new System.Drawing.Size(260, 20);
            this.txtRelayServerUrl.TabIndex = 1;
            this.txtRelayServerUrl.Text = "wss://relay.example.com";
            // 
            // lblRelayServer
            // 
            this.lblRelayServer.AutoSize = true;
            this.lblRelayServer.Location = new System.Drawing.Point(15, 28);
            this.lblRelayServer.Name = "lblRelayServer";
            this.lblRelayServer.Size = new System.Drawing.Size(95, 13);
            this.lblRelayServer.TabIndex = 0;
            this.lblRelayServer.Text = "Relay Server URL:";
            // 
            // groupBoxOptions
            // 
            this.groupBoxOptions.Controls.Add(this.chkAutoStartRelay);
            this.groupBoxOptions.Location = new System.Drawing.Point(12, 138);
            this.groupBoxOptions.Name = "groupBoxOptions";
            this.groupBoxOptions.Size = new System.Drawing.Size(400, 60);
            this.groupBoxOptions.TabIndex = 1;
            this.groupBoxOptions.TabStop = false;
            this.groupBoxOptions.Text = "Options";
            // 
            // chkAutoStartRelay
            // 
            this.chkAutoStartRelay.AutoSize = true;
            this.chkAutoStartRelay.Location = new System.Drawing.Point(15, 25);
            this.chkAutoStartRelay.Name = "chkAutoStartRelay";
            this.chkAutoStartRelay.Size = new System.Drawing.Size(239, 17);
            this.chkAutoStartRelay.TabIndex = 0;
            this.chkAutoStartRelay.Text = "Automatically start relay mode when server starts";
            this.chkAutoStartRelay.UseVisualStyleBackColor = true;
            // 
            // groupBoxStatus
            // 
            this.groupBoxStatus.Controls.Add(this.lblStatus);
            this.groupBoxStatus.Controls.Add(this.btnStartStop);
            this.groupBoxStatus.Location = new System.Drawing.Point(12, 204);
            this.groupBoxStatus.Name = "groupBoxStatus";
            this.groupBoxStatus.Size = new System.Drawing.Size(400, 80);
            this.groupBoxStatus.TabIndex = 2;
            this.groupBoxStatus.TabStop = false;
            this.groupBoxStatus.Text = "Status";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(15, 25);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(92, 13);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "DISCONNECTED";
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(120, 45);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(160, 23);
            this.btnStartStop.TabIndex = 0;
            this.btnStartStop.Text = "Start Relay";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(248, 290);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(337, 290);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // FrmRelaySettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(424, 325);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.groupBoxStatus);
            this.Controls.Add(this.groupBoxOptions);
            this.Controls.Add(this.groupBoxConnection);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmRelaySettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Connection Settings";
            this.Load += new System.EventHandler(this.FrmRelaySettings_Load);
            this.groupBoxConnection.ResumeLayout(false);
            this.groupBoxConnection.PerformLayout();
            this.groupBoxOptions.ResumeLayout(false);
            this.groupBoxOptions.PerformLayout();
            this.groupBoxStatus.ResumeLayout(false);
            this.groupBoxStatus.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxConnection;
        private System.Windows.Forms.TextBox txtRelayPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtRelayDeviceId;
        private System.Windows.Forms.Label lblDeviceId;
        private System.Windows.Forms.TextBox txtRelayServerUrl;
        private System.Windows.Forms.Label lblRelayServer;
        private System.Windows.Forms.GroupBox groupBoxOptions;
        private System.Windows.Forms.CheckBox chkAutoStartRelay;
        private System.Windows.Forms.GroupBox groupBoxStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
