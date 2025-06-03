using Quasar.Common.Relay.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Quasar.Common.Relay.Services
{
    /// <summary>
    /// Delegate for handling relay messages
    /// </summary>
    /// <param name="message">The received message</param>
    public delegate void RelayMessageHandler(RelayMessage message);

    /// <summary>
    /// Client for connecting to the Quasar relay server
    /// </summary>
    public class RelayClient : IDisposable
    {
        private WebSocket _webSocket;
        private readonly RelayConnection _connection;
        private Timer _heartbeatTimer;
        private bool _isDisposed;
        private const int HeartbeatInterval = 30000; // 30 seconds

        /// <summary>
        /// Event raised when a message is received from the relay server
        /// </summary>
        public event RelayMessageHandler MessageReceived;

        /// <summary>
        /// Event raised when the connection status changes
        /// </summary>
        public event EventHandler<ConnectionStatus> StatusChanged;

        /// <summary>
        /// Gets the current connection status
        /// </summary>
        public ConnectionStatus Status
        {
            get { return _connection.Status; }
            private set
            {
                if (_connection.Status != value)
                {
                    _connection.Status = value;
                    StatusChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets the device ID for this connection
        /// </summary>
        public string DeviceId => _connection.DeviceId;

        /// <summary>
        /// Gets the session ID for the current connection
        /// </summary>
        public string SessionId => _connection.SessionId;

        /// <summary>
        /// Creates a new relay client
        /// </summary>
        /// <param name="connection">The connection configuration</param>
        public RelayClient(RelayConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Connects to the relay server
        /// </summary>
        /// <returns>True if connected successfully, otherwise false</returns>
        public bool Connect()
        {
            if (_webSocket != null)
            {
                if (_webSocket.ReadyState == WebSocketState.Open)
                    return true;

                _webSocket.Close();
                _webSocket = null;
            }

            try
            {
                _webSocket = new WebSocket(_connection.RelayServerUrl);
                _webSocket.OnMessage += WebSocket_OnMessage;
                _webSocket.OnOpen += WebSocket_OnOpen;
                _webSocket.OnClose += WebSocket_OnClose;
                _webSocket.OnError += WebSocket_OnError;
                _webSocket.Connect();

                return _webSocket.ReadyState == WebSocketState.Open;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to relay server: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the relay server
        /// </summary>
        public void Disconnect()
        {
            StopHeartbeat();

            if (_webSocket != null)
            {
                _webSocket.Close();
                _webSocket = null;
            }

            Status = ConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Registers this device with the relay server
        /// </summary>
        /// <returns>A task that completes when registration is done</returns>
        public Task<bool> RegisterAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open)
            {
                tcs.SetResult(false);
                return tcs.Task;
            }

            var handler = new RelayMessageHandler((message) =>
            {
                if (message is RegisterResponseMessage response)
                {
                    MessageReceived -= handler;

                    if (response.Success)
                    {
                        _connection.DeviceId = response.Id;
                        Status = ConnectionStatus.Connected;
                        StartHeartbeat();
                        tcs.SetResult(true);
                    }
                    else
                    {
                        Console.WriteLine($"Registration failed: {response.Error}");
                        tcs.SetResult(false);
                    }
                }
            });

            MessageReceived += handler;

            var registerMessage = new RegisterMessage
            {
                Id = _connection.DeviceId,
                Password = _connection.Password,
                DeviceType = _connection.DeviceType,
                Name = _connection.DeviceName
            };

            SendMessage(registerMessage);

            // Set a timeout for registration
            Task.Delay(10000).ContinueWith(t =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    MessageReceived -= handler;
                    tcs.SetResult(false);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Connects to a remote device
        /// </summary>
        /// <param name="remoteId">The ID of the remote device to connect to</param>
        /// <returns>A task that completes when the connection request is processed</returns>
        public Task<bool> ConnectToDeviceAsync(string remoteId)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (_webSocket == null || _webSocket.ReadyState != WebSocketState.Open || Status != ConnectionStatus.Connected)
            {
                tcs.SetResult(false);
                return tcs.Task;
            }

            _connection.RemoteId = remoteId;
            Status = ConnectionStatus.Connecting;

            var handler = new RelayMessageHandler((message) =>
            {
                if (message is ConnectResponseMessage response)
                {
                    MessageReceived -= handler;

                    if (response.Success)
                    {
                        _connection.SessionId = response.SessionId;
                        tcs.SetResult(true);
                    }
                    else
                    {
                        Status = ConnectionStatus.Connected;
                        Console.WriteLine($"Connection request failed: {response.Error}");
                        tcs.SetResult(false);
                    }
                }
            });

            MessageReceived += handler;

            var connectMessage = new ConnectMessage
            {
                TargetId = remoteId,
                SessionId = _connection.SessionId
            };

            SendMessage(connectMessage);

            // Set a timeout for connection request
            Task.Delay(10000).ContinueWith(t =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    MessageReceived -= handler;
                    Status = ConnectionStatus.Connected;
                    tcs.SetResult(false);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Sends a WebRTC offer to the remote device
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="targetId">The target device ID</param>
        /// <param name="sdp">The SDP offer</param>
        public void SendOffer(string sessionId, string targetId, string sdp)
        {
            var message = new SignalingMessage
            {
                Type = RelayMessageType.Offer,
                SessionId = sessionId,
                TargetId = targetId,
                Sdp = sdp
            };

            SendMessage(message);
        }

        /// <summary>
        /// Sends a WebRTC answer to the remote device
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="targetId">The target device ID</param>
        /// <param name="sdp">The SDP answer</param>
        public void SendAnswer(string sessionId, string targetId, string sdp)
        {
            var message = new SignalingMessage
            {
                Type = RelayMessageType.Answer,
                SessionId = sessionId,
                TargetId = targetId,
                Sdp = sdp
            };

            SendMessage(message);
        }

        /// <summary>
        /// Sends an ICE candidate to the remote device
        /// </summary>
        /// <param name="sessionId">The session ID</param>
        /// <param name="targetId">The target device ID</param>
        /// <param name="candidate">The ICE candidate</param>
        /// <param name="sdpMid">The SDP mid attribute</param>
        /// <param name="sdpMLineIndex">The SDP m-line index</param>
        public void SendIceCandidate(string sessionId, string targetId, string candidate, string sdpMid, int sdpMLineIndex)
        {
            var message = new SignalingMessage
            {
                Type = RelayMessageType.IceCandidate,
                SessionId = sessionId,
                TargetId = targetId,
                Candidate = candidate,
                SdpMid = sdpMid,
                SdpMLineIndex = sdpMLineIndex
            };

            SendMessage(message);
        }

        /// <summary>
        /// Sends a message to the relay server
        /// </summary>
        /// <param name="message">The message to send</param>
        private void SendMessage(RelayMessage message)
        {
            if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
            {
                _webSocket.Send(message.ToJson());
            }
        }

        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                var message = RelayMessage.FromJson(e.Data);
                MessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            Console.WriteLine("Connected to relay server");
        }

        private void WebSocket_OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine($"Disconnected from relay server: {e.Reason}");
            StopHeartbeat();
            Status = ConnectionStatus.Disconnected;
        }

        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"WebSocket error: {e.Message}");
        }

        private void StartHeartbeat()
        {
            StopHeartbeat();

            _heartbeatTimer = new Timer(SendHeartbeat, null, 0, HeartbeatInterval);
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private void SendHeartbeat(object state)
        {
            if (_webSocket != null && _webSocket.ReadyState == WebSocketState.Open)
            {
                _connection.LastHeartbeat = DateTime.UtcNow;
                SendMessage(new HeartbeatMessage());
            }
        }

        /// <summary>
        /// Disposes resources used by the relay client
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by the relay client
        /// </summary>
        /// <param name="disposing">Whether this is being called from Dispose or a finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disconnect();
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~RelayClient()
        {
            Dispose(false);
        }
    }
}
