using Quasar.Common.Messages;
using Quasar.Common.Relay.Models;
using System;
using System.Threading.Tasks;

namespace Quasar.Common.Relay.Services
{
    /// <summary>
    /// Client for connecting to the Quasar relay server
    /// </summary>
    public class RelayClient
    {
        /// <summary>
        /// Delegate for handling relay connection status changes
        /// </summary>
        /// <param name="status">The new connection status</param>
        public delegate void StatusChangedEventHandler(ConnectionStatus status);

        /// <summary>
        /// Delegate for handling relay messages
        /// </summary>
        /// <param name="message">The relay message</param>
        public delegate void MessageReceivedEventHandler(IMessage message);

        /// <summary>
        /// Event fired when the connection status changes
        /// </summary>
        public event StatusChangedEventHandler StatusChanged;

        /// <summary>
        /// Event fired when a message is received
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        /// <summary>
        /// The device ID assigned by the relay server
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// The current connection status
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Disconnected;

        /// <summary>
        /// The underlying relay manager that handles the WebSocket connection
        /// </summary>
        private RelayManager _relayManager;

        /// <summary>
        /// Creates a new relay client
        /// </summary>
        /// <param name="serverUrl">The relay server URL (e.g., wss://relay.nextcloudcyber.com)</param>
        /// <param name="password">The password for authentication</param>
        public RelayClient(string serverUrl = "wss://relay.nextcloudcyber.com", string password = "test-password")
        {
            _relayManager = new RelayManager()
            {
                ServerUrl = serverUrl,
                Password = password
            };
        }

        /// <summary>
        /// Connects to the relay server
        /// </summary>
        public async Task ConnectAsync(string deviceName)
        {
            try
            {
                await _relayManager.ConnectAsync(deviceName);
                DeviceId = _relayManager.DeviceId;
                UpdateStatus(ConnectionStatus.Authenticated);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to relay server: {ex.Message}");
                UpdateStatus(ConnectionStatus.Error);
            }
        }

        /// <summary>
        /// Disconnects from the relay server
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                await _relayManager.DisconnectAsync();
                UpdateStatus(ConnectionStatus.Disconnected);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from relay server: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a message through the relay
        /// </summary>
        public async Task SendMessageAsync(IMessage message, string targetDeviceId)
        {
            await _relayManager.SendMessageAsync(message, targetDeviceId);
        }

        /// <summary>
        /// Updates the connection status and fires the status changed event
        /// </summary>
        private void UpdateStatus(ConnectionStatus status)
        {
            if (ConnectionStatus != status)
            {
                ConnectionStatus = status;
                StatusChanged?.Invoke(status);
            }
        }
    }
}
