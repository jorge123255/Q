using Quasar.Common.Messages;
using Quasar.Server.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quasar.Server.Networking
{
    /// <summary>
    /// Listens for client connections through the relay server.
    /// </summary>
    public class RelayListener
    {
        /// <summary>
        /// Delegate for handling new client connections.
        /// </summary>
        /// <param name="client">The connected client.</param>
        /// <param name="endPoint">The client endpoint information.</param>
        public delegate void ClientConnectedEventHandler(Client client, string clientId);

        /// <summary>
        /// Event is fired when a client connected.
        /// </summary>
        public event ClientConnectedEventHandler ClientConnected;

        /// <summary>
        /// The underlying relay server connection.
        /// </summary>
        private readonly RelayServerConnection _relayConnection;

        /// <summary>
        /// Dictionary of connected clients with their relay IDs.
        /// </summary>
        private readonly Dictionary<string, Client> _clients = new Dictionary<string, Client>();

        /// <summary>
        /// Lock object for the clients dictionary.
        /// </summary>
        private readonly object _clientsLock = new object();

        /// <summary>
        /// Gets the device ID used on the relay network.
        /// </summary>
        public string DeviceId => _relayConnection?.DeviceId;

        /// <summary>
        /// Gets whether the relay listener is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayListener"/> class.
        /// </summary>
        public RelayListener()
        {
            _relayConnection = new RelayServerConnection();
            _relayConnection.ClientConnected += OnClientConnected;
            _relayConnection.ClientDisconnected += OnClientDisconnected;
            _relayConnection.MessageReceived += OnMessageReceived;
        }

        /// <summary>
        /// Starts listening for client connections via the relay server.
        /// </summary>
        /// <param name="relayServerUrl">The URL of the relay server.</param>
        /// <param name="deviceId">Optional device ID to use.</param>
        /// <param name="password">Optional password for authentication.</param>
        /// <returns>True if the listener was started successfully, otherwise false.</returns>
        public async Task<bool> StartAsync(string relayServerUrl, string deviceId = null, string password = null)
        {
            if (IsRunning)
                return false;

            bool started = await _relayConnection.StartAsync(relayServerUrl, deviceId, password);
            if (started)
            {
                IsRunning = true;
            }

            return started;
        }

        /// <summary>
        /// Stops listening for client connections.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            _relayConnection.Stop();

            // Disconnect all clients connected via relay
            lock (_clientsLock)
            {
                foreach (var client in _clients.Values)
                {
                    client.Disconnect();
                }
                _clients.Clear();
            }

            IsRunning = false;
        }

        /// <summary>
        /// Handles a new client connection through the relay.
        /// </summary>
        /// <param name="connection">The relay connection.</param>
        /// <param name="clientId">The ID of the connected client.</param>
        private void OnClientConnected(RelayServerConnection connection, string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return;

            // Create a new client for this relay connection
            var client = new RelayClient(connection, clientId);
            
            lock (_clientsLock)
            {
                _clients[clientId] = client;
            }

            // Notify listeners of the new connection
            ClientConnected?.Invoke(client, clientId);
        }

        /// <summary>
        /// Handles a client disconnection from the relay.
        /// </summary>
        /// <param name="connection">The relay connection.</param>
        /// <param name="clientId">The ID of the disconnected client.</param>
        private void OnClientDisconnected(RelayServerConnection connection, string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return;

            Client client;
            lock (_clientsLock)
            {
                if (!_clients.TryGetValue(clientId, out client))
                    return;

                _clients.Remove(clientId);
            }

            // Disconnect the client
            client?.Disconnect();
        }

        /// <summary>
        /// Handles a message received from a client via the relay.
        /// </summary>
        /// <param name="connection">The relay connection.</param>
        /// <param name="clientId">The ID of the client that sent the message.</param>
        /// <param name="message">The received message.</param>
        private void OnMessageReceived(RelayServerConnection connection, string clientId, IMessage message)
        {
            if (string.IsNullOrEmpty(clientId) || message == null)
                return;

            Client client;
            lock (_clientsLock)
            {
                if (!_clients.TryGetValue(clientId, out client))
                    return;
            }

            // Process the message through the client
            ((RelayClient)client).ProcessMessage(message);
        }
    }
}
