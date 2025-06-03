using Quasar.Common.Messages;
using System;

namespace Quasar.Server.Networking
{
    /// <summary>
    /// Client implementation for connections established via the relay server.
    /// </summary>
    public class RelayClient : Client
    {
        /// <summary>
        /// The relay server connection handling this client.
        /// </summary>
        private readonly RelayServerConnection _relayConnection;

        /// <summary>
        /// The client's ID on the relay network.
        /// </summary>
        private readonly string _clientId;

        /// <summary>
        /// Gets the relay client ID for this connection.
        /// </summary>
        public string RelayClientId => _clientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayClient"/> class.
        /// </summary>
        /// <param name="relayConnection">The relay server connection handling this client.</param>
        /// <param name="clientId">The client's ID on the relay network.</param>
        public RelayClient(RelayServerConnection relayConnection, string clientId)
        {
            _relayConnection = relayConnection ?? throw new ArgumentNullException(nameof(relayConnection));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        /// <summary>
        /// Processes a message received from the client via the relay.
        /// </summary>
        /// <param name="message">The message to process.</param>
        public void ProcessMessage(IMessage message)
        {
            if (message == null)
                return;

            // Create a message length (not actually used in relay mode)
            int messageLength = 0;

            // Notify that a message was received
            OnClientRead(message, messageLength);
        }

        /// <summary>
        /// Sends a message to the connected client.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to be sent.</param>
        public override void Send<T>(T message)
        {
            if (message == null || !Connected)
                return;

            try
            {
                // Send the message through the relay connection
                bool sent = _relayConnection.SendMessage(_clientId, message);
                
                if (sent)
                {
                    // Notify that a message was sent
                    OnClientWrite(message, 0);
                }
            }
            catch (Exception ex)
            {
                // Notify of client failure
                OnClientFail(ex);
            }
        }

        /// <summary>
        /// Disconnect the client.
        /// </summary>
        public override void Disconnect()
        {
            // In a full implementation, we would send a disconnect message
            // through the relay connection
            
            // Set the client state to disconnected
            Connected = false;
            
            // Notify that the client disconnected
            OnClientState(false);
        }
    }
}
