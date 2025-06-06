using System;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents a message sent through the relay server.
    /// Contains the encrypted message data and metadata.
    /// </summary>
    [Serializable]
    public class RelayMessage
    {
        /// <summary>
        /// The ID of the target device to send the message to
        /// </summary>
        public string TargetDeviceId { get; set; }
        
        /// <summary>
        /// The full type name of the contained message
        /// </summary>
        public string MessageType { get; set; }
        
        /// <summary>
        /// The type of relay message
        /// </summary>
        public RelayMessageType Type { get; set; }
        
        /// <summary>
        /// The encrypted message data
        /// </summary>
        public byte[] MessageData { get; set; }
    }
}
