using System;
using System.Runtime.Serialization;
using Quasar.Common.Utilities;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents a connection request message for relay communication
    /// </summary>
    [DataContract]
    public class ConnectionRequestMessage
    {
        /// <summary>
        /// The source device ID
        /// </summary>
        [DataMember]
        public string SourceDeviceId { get; set; }
        
        /// <summary>
        /// The target device ID
        /// </summary>
        [DataMember]
        public string TargetDeviceId { get; set; }
        
        /// <summary>
        /// The name of the source device
        /// </summary>
        [DataMember]
        public string SourceName { get; set; }
        
        /// <summary>
        /// The timestamp of the message
        /// </summary>
        [DataMember]
        public long Timestamp { get; set; }

        /// <summary>
        /// Creates a new connection request message
        /// </summary>
        public ConnectionRequestMessage()
        {
            Timestamp = DateTime.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
