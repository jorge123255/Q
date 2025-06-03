using Quasar.Common.Relay.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Quasar.Common.Networking
{
    /// <summary>
    /// Provides security services for relay connections including encryption, rate limiting, and auditing.
    /// Implements end-to-end encryption, rate limiting, and comprehensive security logging.
    /// </summary>
    public class RelaySecurityProvider
    {
        // Rate limiting parameters
        private readonly int _maxRequestsPerMinute;
        private readonly int _maxRequestsPerSecond;
        private int _requestCount;
        private int _secondRequestCount;
        private DateTime _lastResetTime;
        private DateTime _lastSecondResetTime;
        private readonly object _rateLimitLock = new object();
        private Dictionary<string, ClientRateLimit> _clientRateLimits = new Dictionary<string, ClientRateLimit>();

        // Encryption parameters
        private byte[] _encryptionKey;
        private byte[] _encryptionIv;
        private readonly TimeSpan _keyRotationInterval;
        private DateTime _lastKeyRotationTime;

        /// <summary>
        /// Event triggered when a rate limit is exceeded.
        /// </summary>
        public event EventHandler RateLimitExceeded;

        /// <summary>
        /// Event triggered when a security-related audit event occurs.
        /// </summary>
        public event EventHandler<string> SecurityAuditEvent;
        
        /// <summary>
        /// Audit logger for relay security events
        /// </summary>
        private readonly RelayAuditLogger _auditLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelaySecurityProvider"/> class.
        /// </summary>
        /// <param name="encryptionKey">The encryption key (if null, a secure random key will be generated).</param>
        /// <param name="encryptionIv">The encryption IV (if null, a secure random IV will be generated).</param>
        /// <param name="maxRequestsPerMinute">Maximum number of requests allowed per minute.</param>
        /// <param name="maxRequestsPerSecond">Maximum number of requests allowed per second.</param>
        /// <param name="keyRotationIntervalHours">Hours between encryption key rotations.</param>
        /// <param name="logDirectory">Directory for security audit logs. If null, uses default application directory.</param>
        public RelaySecurityProvider(
            byte[] encryptionKey = null, 
            byte[] encryptionIv = null, 
            int maxRequestsPerMinute = 300, 
            int maxRequestsPerSecond = 10,
            int keyRotationIntervalHours = 24,
            string logDirectory = null)
        {
            // Rate limiting settings
            _maxRequestsPerMinute = maxRequestsPerMinute;
            _maxRequestsPerSecond = maxRequestsPerSecond;
            _requestCount = 0;
            _secondRequestCount = 0;
            _lastResetTime = DateTime.UtcNow;
            _lastSecondResetTime = DateTime.UtcNow;
            
            // Encryption settings
            _encryptionKey = encryptionKey ?? GenerateSecureKey(32); // 256-bit key
            _encryptionIv = encryptionIv ?? GenerateSecureKey(16); // 128-bit IV
            _keyRotationInterval = TimeSpan.FromHours(keyRotationIntervalHours);
            _lastKeyRotationTime = DateTime.UtcNow;

            // Initialize the audit logger
            _auditLogger = new RelayAuditLogger(logDirectory);
            _auditLogger.AuditEntryLogged += (s, e) => SecurityAuditEvent?.Invoke(this, e);

            // Start timers for rate limiting and key rotation
            var minuteTimer = new Timer(ResetRequestCount, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            var secondTimer = new Timer(ResetSecondRequestCount, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            var keyRotationTimer = new Timer(RotateEncryptionKey, null, _keyRotationInterval, _keyRotationInterval);
            
            LogAuditEvent("RelaySecurityProvider initialized");
        }

        /// <summary>
        /// Encrypts the provided data using AES encryption.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public byte[] EncryptData(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = _encryptionIv;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                    return memoryStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Decrypts the provided data using AES decryption.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public byte[] DecryptData(byte[] encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = _encryptionIv;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var memoryStream = new MemoryStream(encryptedData))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var resultStream = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int read;
                    while ((read = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        resultStream.Write(buffer, 0, read);
                    }
                    return resultStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Checks if the current request can be processed based on rate limiting rules.
        /// </summary>
        /// <param name="deviceId">Optional device ID for per-client rate limiting</param>
        /// <returns>True if the request is allowed, false otherwise.</returns>
        public bool CheckRateLimit(string deviceId = null)
        {
            lock (_rateLimitLock)
            {
                // Check global rate limits
                bool globalAllowed = CheckGlobalRateLimit();
                
                // If no device ID provided or global limit exceeded, return the global result
                if (string.IsNullOrEmpty(deviceId) || !globalAllowed)
                    return globalAllowed;
                    
                // Check client-specific rate limit
                return CheckClientRateLimit(deviceId);
            }
        }
        
        /// <summary>
        /// Checks the global rate limit for all requests
        /// </summary>
        private bool CheckGlobalRateLimit()
        {
            // Reset minute counter if needed
            if ((DateTime.UtcNow - _lastResetTime).TotalMinutes >= 1)
            {
                _requestCount = 0;
                _lastResetTime = DateTime.UtcNow;
            }
            
            // Reset second counter if needed
            if ((DateTime.UtcNow - _lastSecondResetTime).TotalSeconds >= 1)
            {
                _secondRequestCount = 0;
                _lastSecondResetTime = DateTime.UtcNow;
            }

            // Check minute rate limit
            if (_requestCount >= _maxRequestsPerMinute)
            {
                LogAuditEvent($"Global rate limit exceeded: {_requestCount} requests in the last minute");
                RateLimitExceeded?.Invoke(this, EventArgs.Empty);
                return false;
            }
            
            // Check second rate limit
            if (_secondRequestCount >= _maxRequestsPerSecond)
            {
                LogAuditEvent($"Global rate limit exceeded: {_secondRequestCount} requests in the last second");
                RateLimitExceeded?.Invoke(this, EventArgs.Empty);
                return false;
            }

            // Increment request counts
            _requestCount++;
            _secondRequestCount++;
            return true;
        }
        
        /// <summary>
        /// Checks client-specific rate limits
        /// </summary>
        private bool CheckClientRateLimit(string deviceId)
        {
            // Get or create client rate limit
            if (!_clientRateLimits.TryGetValue(deviceId, out ClientRateLimit clientLimit))
            {
                clientLimit = new ClientRateLimit();
                _clientRateLimits[deviceId] = clientLimit;
            }
            
            // Check if per-client rate limit is exceeded
            if (clientLimit.IsLimitExceeded())
            {
                LogAuditEvent($"Client rate limit exceeded for device: {deviceId}");
                _auditLogger.LogRateLimitExceeded(deviceId, clientLimit.RequestCount, 60);
                RateLimitExceeded?.Invoke(this, EventArgs.Empty);
                return false;
            }
            
            // Increment client request count
            clientLimit.IncrementRequestCount();
            return true;
        }

        /// <summary>
        /// Logs a security audit event.
        /// </summary>
        /// <param name="eventMessage">The audit event message.</param>
        /// <param name="deviceId">Optional device ID associated with the event.</param>
        public void LogAuditEvent(string eventMessage, string deviceId = null)
        {
            _auditLogger.LogAuditEvent(eventMessage, AuditEventType.Security, deviceId);
        }

        /// <summary>
        /// Generates a secure random key of the specified length.
        /// </summary>
        /// <param name="length">The length of the key to generate.</param>
        /// <returns>A secure random key.</returns>
        private byte[] GenerateSecureKey(int length)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] key = new byte[length];
                rng.GetBytes(key);
                return key;
            }
        }

        /// <summary>
        /// Resets the request count for minute-based rate limiting.
        /// </summary>
        private void ResetRequestCount(object state)
        {
            lock (_rateLimitLock)
            {
                _requestCount = 0;
                _lastResetTime = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Resets the request count for second-based rate limiting.
        /// </summary>
        private void ResetSecondRequestCount(object state)
        {
            lock (_rateLimitLock)
            {
                _secondRequestCount = 0;
                _lastSecondResetTime = DateTime.UtcNow;
            }
        }
        
        /// <summary>
        /// Rotates the encryption key and IV for added security.
        /// </summary>
        private void RotateEncryptionKey(object state)
        {
            lock (_rateLimitLock) // Reuse the same lock for thread safety
            {
                // Generate new key and IV
                _encryptionKey = GenerateSecureKey(32);
                _encryptionIv = GenerateSecureKey(16);
                _lastKeyRotationTime = DateTime.UtcNow;
                
                LogAuditEvent("Encryption keys rotated for enhanced security");
            }
        }
        
        /// <summary>
        /// Represents rate limiting data for a specific client.
        /// </summary>
        private class ClientRateLimit
        {
            private const int DEFAULT_MAX_REQUESTS = 100;
            private const int COOLDOWN_MINUTES = 5;
            
            public int RequestCount { get; private set; }
            public DateTime LastResetTime { get; private set; }
            public bool IsCoolingDown { get; private set; }
            public DateTime CooldownUntil { get; private set; }
            
            public ClientRateLimit()
            {
                RequestCount = 0;
                LastResetTime = DateTime.UtcNow;
                IsCoolingDown = false;
                CooldownUntil = DateTime.UtcNow;
            }
            
            public bool IsLimitExceeded()
            {
                // Reset counter if a minute has passed
                if ((DateTime.UtcNow - LastResetTime).TotalMinutes >= 1)
                {
                    RequestCount = 0;
                    LastResetTime = DateTime.UtcNow;
                }
                
                // If in cooldown period, deny requests
                if (IsCoolingDown)
                {
                    if (DateTime.UtcNow < CooldownUntil)
                        return true;
                        
                    // Cooldown period has expired
                    IsCoolingDown = false;
                }
                
                // Check if client-specific rate limit is exceeded
                if (RequestCount >= DEFAULT_MAX_REQUESTS)
                {
                    // Place client in cooldown
                    IsCoolingDown = true;
                    CooldownUntil = DateTime.UtcNow.AddMinutes(COOLDOWN_MINUTES);
                    return true;
                }
                
                return false;
            }
            
            public void IncrementRequestCount()
            {
                RequestCount++;
            }
        }
    }
}
