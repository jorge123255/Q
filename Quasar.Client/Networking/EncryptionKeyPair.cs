using System;

namespace Quasar.Client.Networking
{
    /// <summary>
    /// Represents an encryption key pair with a key and initialization vector
    /// </summary>
    public class EncryptionKeyPair
    {
        /// <summary>
        /// The encryption key
        /// </summary>
        public byte[] Key { get; set; }
        
        /// <summary>
        /// The initialization vector
        /// </summary>
        public byte[] IV { get; set; }
        
        /// <summary>
        /// Creates a new encryption key pair
        /// </summary>
        public EncryptionKeyPair(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;
        }
    }
}
