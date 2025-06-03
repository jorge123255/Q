using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Quasar.Relay.Tests.LoadTesting
{
    /// <summary>
    /// Load testing utility for the Quasar Relay server. Tests connection handling, 
    /// rate limiting, and encryption under high connection volumes.
    /// </summary>
    public class RelayLoadTest
    {
        private readonly string _relayServerUrl;
        private readonly int _maxConnections;
        private readonly int _connectionRate;
        private readonly bool _useEncryption;
        private readonly string _password;
        private readonly int _messageSize;
        private readonly int _messageBurstCount;
        private readonly int _testDurationSeconds;
        
        private int _successfulConnections = 0;
        private int _failedConnections = 0;
        private int _rateLimitedConnections = 0;
        private int _encryptionFailures = 0;
        private int _messagesSent = 0;
        private int _messagesReceived = 0;
        
        private readonly List<ClientConnection> _connections = new List<ClientConnection>();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly object _lockObj = new object();

        /// <summary>
        /// Creates a new relay load testing instance
        /// </summary>
        /// <param name="relayServerUrl">The URL of the relay server to test (wss://your-domain.com)</param>
        /// <param name="maxConnections">Maximum number of concurrent connections to establish</param>
        /// <param name="connectionRate">Number of connections to establish per second</param>
        /// <param name="useEncryption">Whether to use encryption for connections</param>
        /// <param name="password">Password to use for encryption (required if useEncryption is true)</param>
        /// <param name="messageSize">Size of test messages in bytes</param>
        /// <param name="messageBurstCount">Number of messages to send in bursts per connection</param>
        /// <param name="testDurationSeconds">Total test duration in seconds</param>
        public RelayLoadTest(string relayServerUrl, int maxConnections = 100, int connectionRate = 10, 
            bool useEncryption = true, string password = "LoadTestPassword123!", int messageSize = 1024, 
            int messageBurstCount = 5, int testDurationSeconds = 60)
        {
            _relayServerUrl = relayServerUrl;
            _maxConnections = maxConnections;
            _connectionRate = connectionRate;
            _useEncryption = useEncryption;
            _password = password;
            _messageSize = messageSize;
            _messageBurstCount = messageBurstCount;
            _testDurationSeconds = testDurationSeconds;
        }

        /// <summary>
        /// Runs the load test against the relay server
        /// </summary>
        public async Task RunTest()
        {
            Console.WriteLine($"Starting load test against {_relayServerUrl}");
            Console.WriteLine($"Configuration: {_maxConnections} max connections, {_connectionRate} conn/sec, " +
                             $"Encryption: {_useEncryption}, Duration: {_testDurationSeconds}s");
            
            _stopwatch.Start();
            var connectionTasks = new List<Task>();
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            
            // Schedule connection creation
            var connectionTask = Task.Run(async () => 
            {
                try 
                {
                    int connectionsCreated = 0;
                    
                    while (connectionsCreated < _maxConnections && 
                           _stopwatch.Elapsed.TotalSeconds < _testDurationSeconds && 
                           !token.IsCancellationRequested)
                    {
                        // Calculate how many connections to create in this batch
                        int batchSize = Math.Min(_connectionRate, _maxConnections - connectionsCreated);
                        var batchTasks = new List<Task>();
                        
                        for (int i = 0; i < batchSize; i++)
                        {
                            var connectTask = CreateAndConnectClient(connectionsCreated + i, token);
                            batchTasks.Add(connectTask);
                            connectionsCreated++;
                        }
                        
                        // Wait for all connections in this batch to be established
                        await Task.WhenAll(batchTasks);
                        
                        // Wait until it's time for the next batch
                        await Task.Delay(1000, token); // Wait 1 second between batches
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Connection creation canceled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in connection creation: {ex.Message}");
                }
            }, token);
            
            connectionTasks.Add(connectionTask);
            
            // Add a monitoring task to print progress
            connectionTasks.Add(Task.Run(async () => 
            {
                while (_stopwatch.Elapsed.TotalSeconds < _testDurationSeconds && !token.IsCancellationRequested)
                {
                    await Task.Delay(5000, token); // Report every 5 seconds
                    ReportProgress();
                }
            }, token));
            
            // Wait for test duration
            await Task.Delay(TimeSpan.FromSeconds(_testDurationSeconds));
            
            // Cancel and clean up
            tokenSource.Cancel();
            try
            {
                await Task.WhenAll(connectionTasks.Where(t => !t.IsCanceled));
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            // Clean up connections
            await CloseAllConnections();
            
            // Final report
            _stopwatch.Stop();
            ReportFinalResults();
        }
        
        private async Task CreateAndConnectClient(int id, CancellationToken token)
        {
            var client = new ClientConnection(id.ToString(), _useEncryption, _password, _messageSize, _messageBurstCount);
            
            try
            {
                lock (_lockObj)
                {
                    _connections.Add(client);
                }
                
                await client.Connect(_relayServerUrl, token);
                
                lock (_lockObj)
                {
                    _successfulConnections++;
                }
                
                // Send initial burst of messages
                await client.SendMessageBurst(token);
                
                // Update message counters
                lock (_lockObj)
                {
                    _messagesSent += client.MessagesSent;
                    _messagesReceived += client.MessagesReceived;
                }
                
                // Schedule periodic message bursts
                _ = Task.Run(async () => 
                {
                    try
                    {
                        while (!token.IsCancellationRequested && client.IsConnected)
                        {
                            await Task.Delay(Random.Shared.Next(5000, 15000), token); // Random interval
                            if (client.IsConnected)
                            {
                                await client.SendMessageBurst(token);
                                
                                // Update message counters
                                lock (_lockObj)
                                {
                                    _messagesSent += client.MessagesSent;
                                    _messagesReceived += client.MessagesReceived;
                                    
                                    // Reset client counters to avoid double-counting
                                    client.ResetCounters();
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in message sending for client {id}: {ex.Message}");
                    }
                }, token);
            }
            catch (Exception ex)
            {
                HandleConnectionError(client, ex);
            }
        }
        
        private void HandleConnectionError(ClientConnection client, Exception ex)
        {
            lock (_lockObj)
            {
                _failedConnections++;
                
                if (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase))
                {
                    _rateLimitedConnections++;
                    Console.WriteLine($"Rate limited: {ex.Message}");
                }
                else if (ex.Message.Contains("encryption", StringComparison.OrdinalIgnoreCase) ||
                         ex.Message.Contains("decrypt", StringComparison.OrdinalIgnoreCase) ||
                         ex.Message.Contains("crypto", StringComparison.OrdinalIgnoreCase))
                {
                    _encryptionFailures++;
                    Console.WriteLine($"Encryption error: {ex.Message}");
                }
                else
                {
                    Console.WriteLine($"Connection error: {ex.Message}");
                }
            }
        }
        
        private async Task CloseAllConnections()
        {
            Console.WriteLine("Closing all connections...");
            var closeTasks = new List<Task>();
            
            foreach (var conn in _connections.Where(c => c.IsConnected))
            {
                closeTasks.Add(conn.Disconnect());
            }
            
            await Task.WhenAll(closeTasks);
            Console.WriteLine("All connections closed.");
        }
        
        private void ReportProgress()
        {
            lock (_lockObj)
            {
                int activeConnections = _connections.Count(c => c.IsConnected);
                Console.WriteLine($"Progress: {_stopwatch.Elapsed.TotalSeconds:F1}s elapsed, " +
                                 $"{activeConnections} active connections, " +
                                 $"{_successfulConnections} successful, " +
                                 $"{_failedConnections} failed, " +
                                 $"{_rateLimitedConnections} rate limited, " +
                                 $"{_messagesSent} sent, " +
                                 $"{_messagesReceived} received");
            }
        }
        
        private void ReportFinalResults()
        {
            lock (_lockObj)
            {
                Console.WriteLine("======= LOAD TEST RESULTS =======");
                Console.WriteLine($"Duration: {_stopwatch.Elapsed.TotalSeconds:F1} seconds");
                Console.WriteLine($"Total Connection Attempts: {_successfulConnections + _failedConnections}");
                Console.WriteLine($"Successful Connections: {_successfulConnections}");
                Console.WriteLine($"Failed Connections: {_failedConnections}");
                Console.WriteLine($"Rate Limited Connections: {_rateLimitedConnections}");
                Console.WriteLine($"Encryption Failures: {_encryptionFailures}");
                Console.WriteLine($"Messages Sent: {_messagesSent}");
                Console.WriteLine($"Messages Received: {_messagesReceived}");
                
                if (_successfulConnections > 0)
                {
                    Console.WriteLine($"Connections per second: {_successfulConnections / _stopwatch.Elapsed.TotalSeconds:F2}");
                    Console.WriteLine($"Messages per second: {_messagesSent / _stopwatch.Elapsed.TotalSeconds:F2}");
                }
                
                Console.WriteLine("================================");
            }
        }

        /// <summary>
        /// Simulated client connection for load testing
        /// </summary>
        private class ClientConnection
        {
            private readonly string _clientId;
            private readonly bool _useEncryption;
            private readonly string _password;
            private readonly int _messageSize;
            private readonly int _messageBurstCount;
            private ClientWebSocket _webSocket;
            private SimpleEncryptionProvider _encryptionProvider;
            
            public bool IsConnected => _webSocket?.State == WebSocketState.Open;
            public int MessagesSent { get; private set; } = 0;
            public int MessagesReceived { get; private set; } = 0;

            public ClientConnection(string clientId, bool useEncryption, string password, int messageSize, int messageBurstCount)
            {
                _clientId = clientId;
                _useEncryption = useEncryption;
                _password = password;
                _messageSize = messageSize;
                _messageBurstCount = messageBurstCount;
                
                if (_useEncryption)
                {
                    _encryptionProvider = new SimpleEncryptionProvider(_password);
                }
            }

            public async Task Connect(string serverUrl, CancellationToken token)
            {
                _webSocket = new ClientWebSocket();
                
                // Add a random client identifier to help with debugging
                _webSocket.Options.SetRequestHeader("X-Client-ID", $"LoadTest-{_clientId}");
                
                try
                {
                    await _webSocket.ConnectAsync(new Uri(serverUrl), token);
                    
                    // Start a background task to receive messages
                    _ = Task.Run(async () => await ReceiveLoop(token), token);
                }
                catch (Exception)
                {
                    throw;
                }
            }

            public async Task SendMessageBurst(CancellationToken token)
            {
                if (!IsConnected) return;
                
                try
                {
                    for (int i = 0; i < _messageBurstCount; i++)
                    {
                        if (!IsConnected || token.IsCancellationRequested) break;
                        
                        // Create a random message of the specified size
                        var message = new
                        {
                            Id = Guid.NewGuid().ToString(),
                            ClientId = _clientId,
                            Timestamp = DateTime.UtcNow,
                            Data = GenerateRandomData(_messageSize)
                        };
                        
                        string json = JsonConvert.SerializeObject(message);
                        byte[] messageBytes = Encoding.UTF8.GetBytes(json);
                        
                        // Encrypt if needed
                        if (_useEncryption && _encryptionProvider != null)
                        {
                            messageBytes = _encryptionProvider.Encrypt(messageBytes);
                        }
                        
                        await _webSocket.SendAsync(
                            new ArraySegment<byte>(messageBytes),
                            WebSocketMessageType.Binary,
                            true,
                            token);
                        
                        MessagesSent++;
                    }
                }
                catch (Exception)
                {
                    // Just let the connection fail
                    await Disconnect();
                }
            }

            private async Task ReceiveLoop(CancellationToken token)
            {
                var buffer = new byte[16384]; // 16KB buffer
                
                try
                {
                    while (IsConnected && !token.IsCancellationRequested)
                    {
                        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await Disconnect();
                            break;
                        }
                        
                        // Process received message
                        byte[] messageBytes = new byte[result.Count];
                        Array.Copy(buffer, messageBytes, result.Count);
                        
                        // Decrypt if needed
                        if (_useEncryption && _encryptionProvider != null)
                        {
                            try
                            {
                                messageBytes = _encryptionProvider.Decrypt(messageBytes);
                            }
                            catch (CryptographicException)
                            {
                                // Decryption failed, might be due to the server not using encryption
                                // or wrong password
                                continue;
                            }
                        }
                        
                        MessagesReceived++;
                    }
                }
                catch (Exception)
                {
                    // Just let the connection fail
                    await Disconnect();
                }
            }

            public async Task Disconnect()
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None);
                    }
                    catch
                    {
                        // Ignore errors during close
                    }
                }
            }

            public void ResetCounters()
            {
                MessagesSent = 0;
                MessagesReceived = 0;
            }

            private string GenerateRandomData(int size)
            {
                // Generate random string data of approximately the requested size
                var random = new Random();
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var data = new StringBuilder(size);
                
                for (int i = 0; i < size; i++)
                {
                    data.Append(chars[random.Next(chars.Length)]);
                }
                
                return data.ToString();
            }
        }
        
        /// <summary>
        /// A simple encryption provider for load testing purposes.
        /// This simulates the RelaySecurityProvider's encryption capabilities.
        /// </summary>
        private class SimpleEncryptionProvider
        {
            private readonly byte[] _key;
            private readonly byte[] _iv;
            
            public SimpleEncryptionProvider(string password)
            {
                // Generate key and IV from password
                using (var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("QuasarRelayLoadTest"), 10000))
                {
                    _key = deriveBytes.GetBytes(32); // 256 bits
                    _iv = deriveBytes.GetBytes(16);  // 128 bits
                }
            }
            
            public byte[] Encrypt(byte[] data)
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var encryptor = aes.CreateEncryptor())
                    using (var ms = new System.IO.MemoryStream())
                    {
                        // Add a simple header to indicate this is encrypted data
                        ms.WriteByte(0xE1); // Encryption marker
                        
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            cs.Write(data, 0, data.Length);
                            cs.FlushFinalBlock();
                        }
                        
                        return ms.ToArray();
                    }
                }
            }
            
            public byte[] Decrypt(byte[] encryptedData)
            {
                // Check for encryption marker
                if (encryptedData.Length < 1 || encryptedData[0] != 0xE1)
                {
                    throw new CryptographicException("Data is not in the expected encrypted format");
                }
                
                // Skip the header
                byte[] cipherText = new byte[encryptedData.Length - 1];
                Array.Copy(encryptedData, 1, cipherText, 0, cipherText.Length);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new System.IO.MemoryStream(cipherText))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var output = new System.IO.MemoryStream())
                    {
                        cs.CopyTo(output);
                        return output.ToArray();
                    }
                }
            }
        }
    }
}
