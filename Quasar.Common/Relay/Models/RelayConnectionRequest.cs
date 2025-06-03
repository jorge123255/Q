using System;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents a request to connect to a remote device via the relay server.
    /// </summary>
    [Serializable]
    public class RelayConnectionRequest
    {
        /// <summary>
        /// The ID of the target device to connect to
        /// </summary>
        public string TargetDeviceId { get; set; }
    }
}
