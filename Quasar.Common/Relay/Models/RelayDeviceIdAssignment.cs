using System;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents a message from the relay server assigning a device ID to a client.
    /// </summary>
    [Serializable]
    public class RelayDeviceIdAssignment
    {
        /// <summary>
        /// The device ID assigned by the relay server
        /// </summary>
        public string DeviceIdAssigned { get; set; }
    }
}
