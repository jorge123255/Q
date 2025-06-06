using Quasar.Common.Messages;
using Quasar.Common.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Manages relay server communications for both client and server.
    /// </summary>
    public class RelayManager
    {
        /// <summary>
        /// Maximum message size for relay communication
        /// </summary>
        private const int MAX_MESSAGE_SIZE = 1024 * 1024; // 1 MB
        
        /// <summary>
        /// The WebSocket client used to communicate with the relay server
        /// </summary>
        private ClientWebSocket _webSocket;
        
        /// <summary>
        /// The relay server URL
        /// </summary>
        private string _relayServerUrl;
        
        /// <summary>
        /// Unique device ID assigned by the relay server
        /// </summary>
        public string DeviceId { get; private set; }
        
        /// <summary>
        /// Indicates if the relay functionality is enabled
        /// </summary>
        public bool RelayEnabled { get; private set; } = false;
        
        /// <summary>
        /// Indicates if connected to the relay server
        /// </summary>
        public bool Connected => _webSocket?.State == WebSocketState.Open;
        
        /// <summary>
        /// Security provider for end-to-end encryption and rate limiting
        /// </summary>
        private RelaySecurityProvider _securityProvider;
        
        /// <summary>
        /// Delegate for handling received messages
        /// </summary>
        private readonly Action<IMessage> _messageHandler;
        
        /// <summary>
        /// Delegate for handling connection state changes
        /// </summary>
        private readonly Action<bool> _connectionStateHandler;
        
        /// <summary>
        /// Cancellation token source for the receive loop
        /// </summary>
        private CancellationTokenSource _receiveCts;
        
        /// <summary>
        /// Mutex for send operations
        /// </summary>
        private readonly Mutex _sendMutex = new Mutex();
        
        /// <summary>
        /// Creates a new instance of the RelayManager class
        /// </summary>
        /// <param name="messageHandler">Handler for received messages</param>
        /// <param name="connectionStateHandler">Handler for connection state changes</param>
        public RelayManager(Action<IMessage> messageHandler, Action<bool> connectionStateHandler)
        {
            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _connectionStateHandler = connectionStateHandler ?? throw new ArgumentNullException(nameof(connectionStateHandler));
            _securityProvider = new RelaySecurityProvider();
            _securityProvider.SecurityAuditEvent += (s, e) => Debug.WriteLine(e);
            _securityProvider.RateLimitExceeded += (s, e) => Debug.WriteLine("Rate limit exceeded for relay connection");
        }
        
        /// <summary>
        /// Initializes the relay manager with the given server URL
        /// </summary>
        /// <param name="relayServerUrl">The relay server URL</param>
        /// <param name="encryptionKey">Optional encryption key for end-to-end encryption</param>
        /// <param name="encryptionIv">Optional encryption IV for end-to-end encryption</param>
        /// <returns>True if initialization was successful, otherwise false</returns>
        public bool Initialize(string relayServerUrl, byte[] encryptionKey = null, byte[] encryptionIv = null)
        {
            if (string.IsNullOrEmpty(relayServerUrl))
                return false;
                
            _relayServerUrl = relayServerUrl;
            
            // If custom encryption is provided, reinitialize the security provider
            if (encryptionKey != null && encryptionIv != null)
            {
                _securityProvider = new RelaySecurityProvider(encryptionKey, encryptionIv);
                _securityProvider.SecurityAuditEvent += (s, e) => Debug.WriteLine(e);
                _securityProvider.RateLimitExceeded += (s, e) => Debug.WriteLine("Rate limit exceeded for relay connection");
            }
            
            RelayEnabled = true;
            return true;
        }
        
        /// <summary>
        /// Connects to the relay server
        /// </summary>
        /// <returns>True if connection was successful, otherwise false</returns>
        public async Task<bool> ConnectToRelayServerAsync()
        {
            if (!RelayEnabled)
                return false;
                
            try
            {
                // Create a new WebSocket client
                _webSocket = new ClientWebSocket();
                
                // Connect to the relay server
                await _webSocket.ConnectAsync(new Uri(_relayServerUrl), CancellationToken.None);
                
                // Start the receive loop
                _receiveCts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
                
                // Log the connection
                _securityProvider.LogAuditEvent("Connected to relay server");
                
                // Notify of connection state change
                _connectionStateHandler(true);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect to relay server: {ex.Message}");
                _securityProvider.LogAuditEvent($"Connection to relay server failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Connects to a remote device via the relay server
        /// </summary>
        /// <param name="targetDeviceId">The ID of the device to connect to</param>
        /// <returns>True if the connection request was sent, otherwise false</returns>
        public async Task<bool> ConnectToRemoteDeviceAsync(string targetDeviceId)
        {
            if (!Connected || string.IsNullOrEmpty(targetDeviceId))
                return false;
                
            try
            {
                // Check rate limit before proceeding
                if (!_securityProvider.CheckRateLimit())
                    return false;
                    
                // Create a connection request message
                var connectRequest = new RelayConnectionRequest { TargetDeviceId = targetDeviceId };
                
                // Send the message
                return await SendRelayMessageAsync(connectRequest);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to request connection to remote device: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sends a message to a connected device via the relay server
        /// </summary>
        /// <param name="targetDeviceId">The ID of the target device</param>
        /// <param name="message">The message to send</param>
        /// <returns>True if the message was sent successfully, otherwise false</returns>
        public async Task<bool> SendMessageToDeviceAsync(string targetDeviceId, IMessage message)
        {
            if (!Connected || string.IsNullOrEmpty(targetDeviceId) || message == null)
                return false;
                
            try
            {
                // Check rate limit before proceeding
                if (!_securityProvider.CheckRateLimit())
                    return false;
                    
                // Create a relay message with the encrypted content
                var relayMessage = new RelayMessage
                {
                    TargetDeviceId = targetDeviceId,
                    MessageType = message.GetType().FullName,
                    // Serialize and encrypt the message
                    MessageData = SerializeAndEncryptMessage(message)
                };
                
                // Send the relay message
                return await SendRelayMessageAsync(relayMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send message to device: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Disconnects from the relay server
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_webSocket == null)
                return;
                
            try
            {
                // Cancel the receive loop
                _receiveCts?.Cancel();
                
                // Close the WebSocket connection
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                }
                
                // Dispose of the WebSocket
                _webSocket.Dispose();
                _webSocket = null;
                
                // Log the disconnection
                _securityProvider.LogAuditEvent("Disconnected from relay server");
                
                // Notify of connection state change
                _connectionStateHandler(false);
                
                RelayEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during disconnect: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Receive loop for incoming WebSocket messages
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[MAX_MESSAGE_SIZE];
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // WebSocket was closed, disconnect
                        await DisconnectAsync();
                        break;
                    }
                    
                    // Process the received message
                    if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                    {
                        string messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        ProcessReceivedMessage(messageJson);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, do nothing
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in receive loop: {ex.Message}");
                
                // Notify of connection state change
                _connectionStateHandler(false);
            }
        }
        
        /// <summary>
        /// Processes a received message from the relay server
        /// </summary>
        private void ProcessReceivedMessage(string messageJson)
        {
            try
            {
                // Parse the message to determine its type
                if (messageJson.Contains("\"DeviceIdAssigned\":"))
                {
                    // Device ID assignment message
                    var deviceIdMessage = JsonConvert.DeserializeObject<RelayDeviceIdAssignment>(messageJson);
                    DeviceId = deviceIdMessage.DeviceIdAssigned;
                    _securityProvider.LogAuditEvent($"Device ID assigned: {DeviceId}");
                }
                else if (messageJson.Contains("\"MessageType\":") && messageJson.Contains("\"MessageData\":"))
                {
                    // Relay message containing an actual command/message
                    var relayMessage = JsonConvert.DeserializeObject<RelayMessage>(messageJson);
                    
                    // Decrypt and deserialize the message
                    var message = DecryptAndDeserializeMessage(relayMessage.MessageData, relayMessage.MessageType);
                    
                    if (message != null)
                    {
                        // Call the message handler
                        _messageHandler(message);
                    }
                }
                else if (messageJson.Contains("\"SourceDeviceId\":") && messageJson.Contains("\"Connected\":"))
                {
                    // Connection status message
                    var connectionStatus = JsonConvert.DeserializeObject<RelayConnectionStatus>(messageJson);
                    
                    // Log the connection status
                    _securityProvider.LogAuditEvent(
                        connectionStatus.Connected 
                            ? $"Connected to device: {connectionStatus.SourceDeviceId}" 
                            : $"Disconnected from device: {connectionStatus.SourceDeviceId}");
                            
                    // Update connection status
                    _connectionStateHandler(connectionStatus.Connected);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing received message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sends a message to the relay server
        /// </summary>
        private async Task<bool> SendRelayMessageAsync<T>(T message)
        {
            if (_webSocket?.State != WebSocketState.Open)
                return false;
                
            try
            {
                // Check rate limit
                if (!_securityProvider.CheckRateLimit())
                    return false;
                    
                // Serialize the message
                byte[] messageBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(message.GetType());
                    serializer.WriteObject(ms, message);
                    messageBytes = ms.ToArray();
                }
                
                // Send the message
                _sendMutex.WaitOne();
                try
                {
                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                        
                    return true;
                }
                finally
                {
                    _sendMutex.ReleaseMutex();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending relay message: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Serializes and encrypts a message
        /// </summary>
        private byte[] SerializeAndEncryptMessage(IMessage message)
        {
            // Serialize the message to JSON
            byte[] messageBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(message.GetType());
                serializer.WriteObject(ms, message);
                messageBytes = ms.ToArray();
            }
            
            // Encrypt the message data
            return _securityProvider.EncryptData(messageBytes);
        }
        
        /// <summary>
        /// Decrypts and deserializes a message
        /// </summary>
        private IMessage DecryptAndDeserializeMessage(byte[] encryptedData, string messageType)
        {
            try
            {
                // Decrypt the message data
                byte[] decryptedData = _securityProvider.DecryptData(encryptedData);
                
                // Deserialize the message
                Type type = Type.GetType(messageType);
                
                if (type == null)
                {
                    Debug.WriteLine($"Unknown message type: {messageType}");
                    return null;
                }
                
                using (MemoryStream ms = new MemoryStream(decryptedData))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
                    return (IMessage)serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decrypting/deserializing message: {ex.Message}");
                return null;
            }
        }
    }
}
