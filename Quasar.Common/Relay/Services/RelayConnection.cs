using Quasar.Common.Relay.Models;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Quasar.Common.Relay.Services
{
    /// <summary>
    /// Represents a connection to the relay server
    /// </summary>
    [DataContract]
    public class RelayConnection
    {
        /// <summary>
        /// The relay server URL
        /// </summary>
        [DataMember]
        public string ServerUrl { get; set; } = "wss://relay.nextcloudcyber.com";
        
        /// <summary>
        /// The device ID used for authentication
        /// </summary>
        [DataMember]
        public string DeviceId { get; set; }
        
        /// <summary>
        /// The device name
        /// </summary>
        [DataMember]
        public string DeviceName { get; set; }
        
        /// <summary>
        /// The password used for authentication
        /// </summary>
        [DataMember]
        public string Password { get; set; } = "test-password";
        
        /// <summary>
        /// Whether relay connection is enabled
        /// </summary>
        [DataMember]
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Whether to use encryption for relay communication
        /// </summary>
        [DataMember]
        public bool UseEncryption { get; set; } = false;
        
        /// <summary>
        /// Maximum attempts to reconnect
        /// </summary>
        [DataMember]
        public int MaxReconnectAttempts { get; set; } = 5;
        
        /// <summary>
        /// Automatically generate a unique device ID if not provided
        /// </summary>
        public void EnsureDeviceId()
        {
            if (string.IsNullOrEmpty(DeviceId))
            {
                DeviceId = Guid.NewGuid().ToString();
            }
        }
    }
}
