using Quasar.Server.Networking;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Quasar.Server.Forms
{
    public partial class FrmRelaySettings : Form
    {
        private readonly QuasarServer _listenServer;

        public FrmRelaySettings(QuasarServer listenServer)
        {
            _listenServer = listenServer;
            InitializeComponent();
            this.Text = "Connection Settings";
            lblStatus.Text = "Configure relay connection settings below (recommended).";
            txtRelayServerUrl.Text = Properties.Settings.Default.RelayServerUrl;
            txtRelayDeviceId.Text = Properties.Settings.Default.RelayDeviceId;
            txtRelayPassword.Text = Properties.Settings.Default.RelayPassword;
            chkAutoStartRelay.Checked = Properties.Settings.Default.RelayAutoStart;
            
            // Set default focus to the relay server URL field
            txtRelayServerUrl.Focus();
            UpdateRelayStatus();
        }

        private void UpdateRelayStatus()
        {
            lblStatus.Text = _listenServer.RelayEnabled ? 
                $"CONNECTED - Device ID: {_listenServer.RelayDeviceId}" : 
                "DISCONNECTED";

            btnStartStop.Text = _listenServer.RelayEnabled ? "Stop Relay" : "Start Relay";
        }

        private async void btnStartStop_Click(object sender, EventArgs e)
        {
            if (_listenServer.RelayEnabled)
            {
                _listenServer.StopRelayMode();
                UpdateRelayStatus();
            }
            else
            {
                await StartRelayAsync();
            }
        }

        private async Task StartRelayAsync()
        {
            btnStartStop.Enabled = false;
            lblStatus.Text = "Connecting...";

            try
            {
                // Save settings
                Properties.Settings.Default.RelayServerUrl = txtRelayServerUrl.Text;
                Properties.Settings.Default.RelayDeviceId = txtRelayDeviceId.Text;
                Properties.Settings.Default.RelayPassword = txtRelayPassword.Text;
                Properties.Settings.Default.RelayAutoStart = chkAutoStartRelay.Checked;
                Properties.Settings.Default.Save();

                // Start relay mode
                bool success = await _listenServer.StartRelayModeAsync(
                    txtRelayServerUrl.Text, 
                    string.IsNullOrEmpty(txtRelayDeviceId.Text) ? null : txtRelayDeviceId.Text,
                    string.IsNullOrEmpty(txtRelayPassword.Text) ? null : txtRelayPassword.Text);

                if (!success)
                {
                    MessageBox.Show("Failed to connect to relay server. Please check your settings and try again.",
                        "Relay Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while connecting to the relay server: {ex.Message}",
                    "Relay Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStartStop.Enabled = true;
                UpdateRelayStatus();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.RelayServerUrl = txtRelayServerUrl.Text;
            Properties.Settings.Default.RelayDeviceId = txtRelayDeviceId.Text;
            Properties.Settings.Default.RelayPassword = txtRelayPassword.Text;
            Properties.Settings.Default.RelayAutoStart = chkAutoStartRelay.Checked;
            Properties.Settings.Default.Save();

            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void FrmRelaySettings_Load(object sender, EventArgs e)
        {
            UpdateRelayStatus();
        }
    }
}
