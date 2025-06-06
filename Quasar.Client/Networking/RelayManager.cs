using Quasar.Common.Messages;
using Quasar.Common.Relay.Models;
using Quasar.Common.Relay.Services;
using System;
using System.Threading.Tasks;

namespace Quasar.Client.Networking
{
    /// <summary>
    /// Manages relay connections for the Quasar client.
    /// This class integrates with the existing Client class to provide seamless relay functionality.
    /// </summary>
    public class RelayManager
    {
        /// <summary>
        /// Delegate for handling relay messages
        /// </summary>
        /// <param name="message">The relay message</param>
        public delegate void RelayMessageHandler(RelayMessage message);
        
        /// <summary>
        /// Delegate for handling relay connection status changes
        /// </summary>
        /// <param name="status">The new connection status</param>
        public delegate void RelayStatusChangedHandler(ConnectionStatus status);
        
        /// <summary>
        /// The message handler callback
        /// </summary>
        private readonly RelayMessageHandler _messageHandler;

        /// <summary>
        /// The relay client used for connecting to the relay server
        /// </summary>
        private RelayClient _relayClient;

        /// <summary>
        /// The relay connection configuration
        /// </summary>
        private RelayConnection _relayConnection;

        /// <summary>
        /// Whether relay mode is enabled
        /// </summary>
        public bool RelayEnabled { get; private set; }

        /// <summary>
        /// The device ID used for the relay connection
        /// </summary>
        public string DeviceId => _relayConnection?.DeviceId;

        /// <summary>
        /// The connection status of the relay client
        /// </summary>
        public ConnectionStatus ConnectionStatus => _relayClient?.Status ?? ConnectionStatus.Disconnected;

        /// <summary>
        /// The status changed handler callback
        /// </summary>
        private readonly RelayStatusChangedHandler _statusChangedHandler;
        
        /// <summary>
        /// Initializes a new instance of the RelayManager class
        /// </summary>
        /// <param name="messageHandler">The message handler callback</param>
        /// <param name="statusChangedHandler">The status changed handler callback</param>
        public RelayManager(RelayMessageHandler messageHandler, RelayStatusChangedHandler statusChangedHandler)
        {
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _statusChangedHandler = statusChangedHandler ?? throw new ArgumentNullException(nameof(statusChangedHandler));
        }

        /// <summary>
        /// Initializes the relay connection
        /// </summary>
        /// <param name="relayServerUrl">The relay server URL</param>
        /// <param name="deviceId">The device ID (optional, will be generated if not provided)</param>
        /// <param name="password">The device password</param>
        /// <returns>True if initialized successfully, otherwise false</returns>
        public bool Initialize(string relayServerUrl, string deviceId = null, string password = null)
        {
            if (string.IsNullOrEmpty(relayServerUrl))
                return false;

            try
            {
                _relayConnection = new RelayConnection
                {
                    RelayServerUrl = relayServerUrl,
                    DeviceId = deviceId,
                    Password = password ?? Guid.NewGuid().ToString("N"),
                    DeviceType = "client",
                    DeviceName = Environment.MachineName,
                    TryDirectConnection = true
                };

                _relayClient = new RelayClient(_relayConnection);
                _relayClient.MessageReceived += OnRelayMessageReceived;
                _relayClient.StatusChanged += OnRelayStatusChanged;

                RelayEnabled = true;
                return true;
            }
            catch (Exception)
            {
                RelayEnabled = false;
                return false;
            }
        }

        /// <summary>
        /// Connects to the relay server
        /// </summary>
        /// <returns>True if connected successfully, otherwise false</returns>
        public async Task<bool> ConnectToRelayServerAsync()
        {
            if (!RelayEnabled || _relayClient == null)
                return false;

            bool connected = _relayClient.Connect(_relayConnection.DeviceName ?? Environment.MachineName);
            if (!connected)
                return false;

            return await _relayClient.RegisterAsync();
        }

        /// <summary>
        /// Connects to a remote device via the relay server
        /// </summary>
        /// <param name="remoteId">The ID of the remote device to connect to</param>
        /// <returns>True if the connection request was successful, otherwise false</returns>
        public async Task<bool> ConnectToRemoteDeviceAsync(string remoteId)
        {
            if (!RelayEnabled || _relayClient == null || string.IsNullOrEmpty(remoteId))
                return false;

            return await _relayClient.ConnectToDeviceAsync(remoteId);
        }

        /// <summary>
        /// Handles received relay messages
        /// </summary>
        /// <param name="message">The received message</param>
        private void OnRelayMessageReceived(IMessage message)
        {
            // Forward the message to the registered handler
            _messageHandler?.Invoke((RelayMessage)message);
            
            // Process message based on type for internal state management
            switch (message.Type)
            {
                case RelayMessageType.ConnectionRequest:
                    // HandleConnectionRequest((ConnectionRequestMessage)message);
                    break;
                case RelayMessageType.Offer:
                    // HandleOffer((SignalingMessage)message);
                    break;
                case RelayMessageType.Answer:
                    // HandleAnswer((SignalingMessage)message);
                    break;
                case RelayMessageType.IceCandidate:
                    // HandleIceCandidate((SignalingMessage)message);
                    break;
            }
        }

        /// <summary>
        /// Handles relay connection status changes
        /// </summary>
        /// <param name="status">The new connection status</param>
        private void OnRelayStatusChanged(ConnectionStatus status)
        {
            // Forward the status change to the registered handler
            _statusChangedHandler?.Invoke(status);
        }

        /// <summary>
        /// Handles an incoming connection request
        /// </summary>
        /// <param name="message">The connection request message</param>
        private void HandleConnectionRequest(ConnectionRequestMessage message)
        {
            // In the client, we typically don't accept incoming connections
            // This will be implemented in the server side
        }

        /// <summary>
        /// Handles an incoming WebRTC offer
        /// </summary>
        /// <param name="message">The offer message</param>
        private void HandleOffer(SignalingMessage message)
        {
            // Will be implemented in a future update
            // This requires WebRTC integration for direct peer connections
        }

        /// <summary>
        /// Handles an incoming WebRTC answer
        /// </summary>
        /// <param name="message">The answer message</param>
        private void HandleAnswer(SignalingMessage message)
        {
            // Will be implemented in a future update
            // This requires WebRTC integration for direct peer connections
        }

        /// <summary>
        /// Handles an incoming ICE candidate
        /// </summary>
        /// <param name="message">The ICE candidate message</param>
        private void HandleIceCandidate(SignalingMessage message)
        {
            // Will be implemented in a future update
            // This requires WebRTC integration for direct peer connections
        }

        /// <summary>
        /// Disconnects from the relay server
        /// </summary>
        public void Disconnect()
        {
            if (_relayClient != null)
            {
                _relayClient.MessageReceived -= OnRelayMessageReceived;
                _relayClient.StatusChanged -= OnRelayStatusChanged;
                _relayClient.Disconnect();
                _relayClient.Dispose();
                _relayClient = null;
            }

            _relayConnection = null;
            RelayEnabled = false;
        }
    }
}
