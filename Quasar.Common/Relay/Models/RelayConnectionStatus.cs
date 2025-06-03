using System;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents a connection status message from the relay server.
    /// Indicates whether a connection to a remote device has been established or terminated.
    /// </summary>
    [Serializable]
    public class RelayConnectionStatus
    {
        /// <summary>
        /// The ID of the source device that connected or disconnected
        /// </summary>
        public string SourceDeviceId { get; set; }
        
        /// <summary>
        /// Indicates whether the connection is established (true) or terminated (false)
        /// </summary>
        public bool Connected { get; set; }
    }
}
