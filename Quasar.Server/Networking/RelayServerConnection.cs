using Quasar.Common.Messages;
using Quasar.Common.Relay.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quasar.Server.Networking
{
    /// <summary>
    /// Handles server connections through a relay server.
    /// This class integrates with the existing Quasar server infrastructure.
    /// </summary>
    public class RelayServerConnection
    {
        /// <summary>
        /// Delegate for handling new client connections through the relay
        /// </summary>
        /// <param name="connection">The connection object</param>
        /// <param name="endPoint">The client endpoint information</param>
        public delegate void ClientConnectedEventHandler(RelayServerConnection connection, string clientId);

        /// <summary>
        /// Event fired when a new client connects through the relay
        /// </summary>
        public event ClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// Delegate for handling client disconnection events
        /// </summary>
        /// <param name="connection">The connection object</param>
        /// <param name="clientId">The ID of the client that disconnected</param>
        public delegate void ClientDisconnectedEventHandler(RelayServerConnection connection, string clientId);

        /// <summary>
        /// Event fired when a client disconnects from the relay
        /// </summary>
        public event ClientDisconnectedEventHandler ClientDisconnected;

        /// <summary>
        /// Delegate for handling messages received from clients
        /// </summary>
        /// <param name="connection">The connection object</param>
        /// <param name="clientId">The ID of the client that sent the message</param>
        /// <param name="message">The message received</param>
        public delegate void MessageReceivedEventHandler(RelayServerConnection connection, string clientId, IMessage message);

        /// <summary>
        /// Event fired when a message is received from a client
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        /// <summary>
        /// The relay client used for server-side relay connections
        /// </summary>
        private RelayManager _relayManager;

        /// <summary>
        /// The server's device ID on the relay network
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Whether the server is currently connected to the relay network
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Connected clients and their connection state
        /// </summary>
        private readonly Dictionary<string, bool> _connectedClients = new Dictionary<string, bool>();

        /// <summary>
        /// Lock object for the connected clients dictionary
        /// </summary>
        private readonly object _connectedClientsLock = new object();

        /// <summary>
        /// Creates a new instance of the RelayServerConnection class
        /// </summary>
        public RelayServerConnection()
        {
            // Will be initialized when Start is called
            _relayManager = null;
            IsConnected = false;
        }

        /// <summary>
        /// Starts the relay server connection
        /// </summary>
        /// <param name="relayServerUrl">The URL of the relay server</param>
        /// <param name="deviceId">The device ID to use (optional, will be generated if not provided)</param>
        /// <param name="password">The password for authentication (optional, will be generated if not provided)</param>
        /// <returns>True if the server was started successfully, otherwise false</returns>
        public async Task<bool> StartAsync(string relayServerUrl, string deviceId = null, string password = null)
        {
            if (string.IsNullOrEmpty(relayServerUrl))
                return false;

            try
            {
                // Create a new relay manager
                _relayManager = new RelayManager(OnRelayMessageReceived, OnRelayStatusChanged);
                
                // Initialize the relay connection
                bool initialized = _relayManager.Initialize(relayServerUrl, deviceId, password);
                if (!initialized)
                    return false;

                // Connect to the relay server
                bool connected = await _relayManager.ConnectToRelayServerAsync();
                if (!connected)
                    return false;

                // Store the device ID for reference
                DeviceId = _relayManager.DeviceId;
                IsConnected = true;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Stops the relay server connection
        /// </summary>
        public void Stop()
        {
            if (_relayManager != null)
            {
                _relayManager.Disconnect();
                _relayManager = null;
            }

            // Clear connected clients
            lock (_connectedClientsLock)
            {
                _connectedClients.Clear();
            }

            IsConnected = false;
            DeviceId = null;
        }

        /// <summary>
        /// Sends a message to a specific client
        /// </summary>
        /// <param name="clientId">The ID of the client to send the message to</param>
        /// <param name="message">The message to send</param>
        /// <returns>True if the message was sent successfully, otherwise false</returns>
        public bool SendMessage(string clientId, IMessage message)
        {
            if (!IsConnected || _relayManager == null || string.IsNullOrEmpty(clientId) || message == null)
                return false;

            // Check if the client is connected
            lock (_connectedClientsLock)
            {
                if (!_connectedClients.ContainsKey(clientId) || !_connectedClients[clientId])
                    return false;
            }

            // In a full implementation, we would convert the IMessage to a relay message
            // and send it through the relay connection
            // This is a placeholder for future implementation
            return true;
        }

        /// <summary>
        /// Handles messages received from the relay server
        /// </summary>
        /// <param name="message">The received message</param>
        private void OnRelayMessageReceived(RelayMessage message)
        {
            if (message == null)
                return;

            try
            {
                switch (message.Type)
                {
                    case RelayMessageType.ConnectionRequest:
                        HandleConnectionRequest((ConnectionRequestMessage)message);
                        break;
                    case RelayMessageType.Disconnect:
                        HandleDisconnect((DisconnectMessage)message);
                        break;
                    case RelayMessageType.Data:
                        HandleDataMessage((DataMessage)message);
                        break;
                }
            }
            catch (Exception)
            {
                // Log exception - in future implementation
            }
        }

        /// <summary>
        /// Handles relay connection status changes
        /// </summary>
        /// <param name="status">The new connection status</param>
        private void OnRelayStatusChanged(ConnectionStatus status)
        {
            IsConnected = status == ConnectionStatus.Ready;

            if (!IsConnected)
            {
                // Clear connected clients on disconnection
                lock (_connectedClientsLock)
                {
                    _connectedClients.Clear();
                }
            }
        }

        /// <summary>
        /// Handles connection requests from clients
        /// </summary>
        /// <param name="message">The connection request message</param>
        private void HandleConnectionRequest(ConnectionRequestMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.SourceId))
                return;

            // Add the client to our connected clients list
            lock (_connectedClientsLock)
            {
                _connectedClients[message.SourceId] = true;
            }

            // Notify listeners of the new connection
            ClientConnected?.Invoke(this, message.SourceId);
        }

        /// <summary>
        /// Handles disconnect messages from clients
        /// </summary>
        /// <param name="message">The disconnect message</param>
        private void HandleDisconnect(DisconnectMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.SourceId))
                return;

            // Remove the client from our connected clients list
            lock (_connectedClientsLock)
            {
                if (_connectedClients.ContainsKey(message.SourceId))
                {
                    _connectedClients.Remove(message.SourceId);
                }
            }

            // Notify listeners of the disconnection
            ClientDisconnected?.Invoke(this, message.SourceId);
        }

        /// <summary>
        /// Handles data messages from clients
        /// </summary>
        /// <param name="message">The data message</param>
        private void HandleDataMessage(DataMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.SourceId) || message.Data == null)
                return;

            // In a full implementation, we would convert the message data to an IMessage
            // For now, this is a placeholder for future implementation
            // IMessage quasarMessage = ConvertDataToQuasarMessage(message.Data);
            
            // if (quasarMessage != null)
            // {
            //     MessageReceived?.Invoke(this, message.SourceId, quasarMessage);
            // }
        }
    }
}
