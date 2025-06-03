using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace Quasar.Common.Relay.Security
{
    /// <summary>
    /// Provides secure storage for relay connection credentials such as device IDs and passwords.
    /// Uses machine-specific encryption to protect sensitive data.
    /// </summary>
    public class SecureCredentialStorage
    {
        private readonly string _storageFilePath;
        private readonly string _encryptionKey;
        private readonly RelayAuditLogger _auditLogger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureCredentialStorage"/> class.
        /// </summary>
        /// <param name="storageDirectory">Directory to store credentials. If null, uses the default application data directory.</param>
        public SecureCredentialStorage(string storageDirectory = null)
        {
            // Set up storage directory
            string directory = storageDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Quasar", "Credentials");
                
            // Ensure directory exists
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            _storageFilePath = Path.Combine(directory, "relay_credentials.dat");
            
            // Generate machine-specific encryption key for local storage
            _encryptionKey = GenerateMachineSpecificKey();
            
            // Initialize audit logger
            _auditLogger = new RelayAuditLogger();
        }
        
        /// <summary>
        /// Stores relay credentials securely.
        /// </summary>
        /// <param name="credentials">The credentials to store.</param>
        /// <returns>True if storage was successful, otherwise false.</returns>
        public bool StoreCredentials(RelayCredentials credentials)
        {
            if (credentials == null)
                return false;
                
            try
            {
                // Serialize credentials
                XmlSerializer serializer = new XmlSerializer(typeof(RelayCredentials));
                using (StringWriter textWriter = new StringWriter())
                {
                    serializer.Serialize(textWriter, credentials);
                    string serializedData = textWriter.ToString();
                    
                    // Encrypt and save
                    byte[] encryptedData = EncryptString(serializedData);
                    File.WriteAllBytes(_storageFilePath, encryptedData);
                    
                    _auditLogger.LogAuditEvent("Credentials stored securely", AuditEventType.Security);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _auditLogger.LogAuditEvent($"Failed to store credentials: {ex.Message}", AuditEventType.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Retrieves relay credentials from secure storage.
        /// </summary>
        /// <returns>The stored credentials, or null if none exist or an error occurred.</returns>
        public RelayCredentials RetrieveCredentials()
        {
            if (!File.Exists(_storageFilePath))
                return null;
                
            try
            {
                // Read and decrypt
                byte[] encryptedData = File.ReadAllBytes(_storageFilePath);
                string decryptedData = DecryptToString(encryptedData);
                
                // Deserialize
                XmlSerializer serializer = new XmlSerializer(typeof(RelayCredentials));
                using (StringReader textReader = new StringReader(decryptedData))
                {
                    RelayCredentials credentials = (RelayCredentials)serializer.Deserialize(textReader);
                    _auditLogger.LogAuditEvent("Credentials retrieved securely", AuditEventType.Security);
                    return credentials;
                }
            }
            catch (Exception ex)
            {
                _auditLogger.LogAuditEvent($"Failed to retrieve credentials: {ex.Message}", AuditEventType.Error);
                return null;
            }
        }
        
        /// <summary>
        /// Deletes stored credentials.
        /// </summary>
        /// <returns>True if deletion was successful or no file existed, otherwise false.</returns>
        public bool DeleteCredentials()
        {
            try
            {
                if (File.Exists(_storageFilePath))
                {
                    File.Delete(_storageFilePath);
                    _auditLogger.LogAuditEvent("Credentials deleted", AuditEventType.Security);
                }
                return true;
            }
            catch (Exception ex)
            {
                _auditLogger.LogAuditEvent($"Failed to delete credentials: {ex.Message}", AuditEventType.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Checks if credentials exist in storage.
        /// </summary>
        /// <returns>True if credentials exist, otherwise false.</returns>
        public bool CredentialsExist()
        {
            return File.Exists(_storageFilePath);
        }
        
        /// <summary>
        /// Encrypts a string using AES with a machine-specific key.
        /// </summary>
        /// <param name="plainText">The string to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        private byte[] EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return new byte[0];
                
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            
            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKeyFromString(_encryptionKey, 32);
                aes.IV = DeriveKeyFromString(_encryptionKey + "IV", 16);
                aes.Padding = PaddingMode.PKCS7;
                
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(
                        memoryStream, 
                        aes.CreateEncryptor(), 
                        CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                    
                    return memoryStream.ToArray();
                }
            }
        }
        
        /// <summary>
        /// Decrypts data to a string using AES with a machine-specific key.
        /// </summary>
        /// <param name="encryptedData">The encrypted data.</param>
        /// <returns>The decrypted string.</returns>
        private string DecryptToString(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return string.Empty;
                
            using (Aes aes = Aes.Create())
            {
                aes.Key = DeriveKeyFromString(_encryptionKey, 32);
                aes.IV = DeriveKeyFromString(_encryptionKey + "IV", 16);
                aes.Padding = PaddingMode.PKCS7;
                
                using (MemoryStream memoryStream = new MemoryStream(encryptedData))
                using (CryptoStream cryptoStream = new CryptoStream(
                    memoryStream, 
                    aes.CreateDecryptor(), 
                    CryptoStreamMode.Read))
                using (StreamReader reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        
        /// <summary>
        /// Generates a machine-specific encryption key based on hardware identifiers.
        /// </summary>
        /// <returns>A machine-specific key.</returns>
        private string GenerateMachineSpecificKey()
        {
            // Use hardware identifiers that are unique to the machine
            string machineId = Environment.MachineName +
                               Environment.ProcessorCount.ToString() +
                               Environment.OSVersion.ToString() +
                               Environment.UserName;
                               
            // Create a stable hash
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
                return Convert.ToBase64String(hashBytes);
            }
        }
        
        /// <summary>
        /// Derives a key of the specified length from a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="length">The required key length in bytes.</param>
        /// <returns>A key of the specified length.</returns>
        private byte[] DeriveKeyFromString(string input, int length)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(
                input, 
                Encoding.UTF8.GetBytes("QuasarRelaySecureStorage"),
                10000))
            {
                return deriveBytes.GetBytes(length);
            }
        }
    }
    
    /// <summary>
    /// Represents secure relay connection credentials.
    /// </summary>
    [Serializable]
    public class RelayCredentials
    {
        /// <summary>
        /// The URL of the relay server.
        /// </summary>
        public string RelayServerUrl { get; set; }
        
        /// <summary>
        /// The device ID for authentication.
        /// </summary>
        public string DeviceId { get; set; }
        
        /// <summary>
        /// The password for authentication.
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// The timestamp when the credentials were created or last updated.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCredentials"/> class.
        /// </summary>
        public RelayCredentials()
        {
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCredentials"/> class with specified values.
        /// </summary>
        /// <param name="relayServerUrl">The URL of the relay server.</param>
        /// <param name="deviceId">The device ID for authentication.</param>
        /// <param name="password">The password for authentication.</param>
        public RelayCredentials(string relayServerUrl, string deviceId, string password)
        {
            RelayServerUrl = relayServerUrl;
            DeviceId = deviceId;
            Password = password;
            Timestamp = DateTime.UtcNow;
        }
    }
}
