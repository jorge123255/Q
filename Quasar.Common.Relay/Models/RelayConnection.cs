using System;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents the connection status with a relay server or peer
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Not connected to relay server
        /// </summary>
        Disconnected,
        
        /// <summary>
        /// Connected to relay server but not to peer
        /// </summary>
        Connected,
        
        /// <summary>
        /// Connection to peer in progress
        /// </summary>
        Connecting,
        
        /// <summary>
        /// Connected to peer (direct or relayed)
        /// </summary>
        Ready
    }

    /// <summary>
    /// Represents a connection via the relay server
    /// </summary>
    public class RelayConnection
    {
        /// <summary>
        /// Gets or sets the relay server URL
        /// </summary>
        public string RelayServerUrl { get; set; }
        
        /// <summary>
        /// Gets or sets the device ID (generated or specified)
        /// </summary>
        public string DeviceId { get; set; }
        
        /// <summary>
        /// Gets or sets the device password
        /// </summary>
        public string Password { get; set; }
        
        /// <summary>
        /// Gets or sets the remote device ID to connect to
        /// </summary>
        public string RemoteId { get; set; }
        
        /// <summary>
        /// Gets or sets the session ID for the current connection
        /// </summary>
        public string SessionId { get; set; }
        
        /// <summary>
        /// Gets or sets the device type (client or server)
        /// </summary>
        public string DeviceType { get; set; }
        
        /// <summary>
        /// Gets or sets the friendly name of this device
        /// </summary>
        public string DeviceName { get; set; }
        
        /// <summary>
        /// Gets or sets the current connection status
        /// </summary>
        public ConnectionStatus Status { get; set; }
        
        /// <summary>
        /// Gets or sets whether to use a direct connection when possible
        /// </summary>
        public bool TryDirectConnection { get; set; }
        
        /// <summary>
        /// Gets or sets the last time a heartbeat was sent
        /// </summary>
        public DateTime LastHeartbeat { get; set; }
        
        /// <summary>
        /// Gets or sets the STUN server URL
        /// </summary>
        public string StunServer { get; set; }
        
        /// <summary>
        /// Gets or sets the TURN server URL
        /// </summary>
        public string TurnServer { get; set; }
        
        /// <summary>
        /// Gets or sets the TURN server username
        /// </summary>
        public string TurnUsername { get; set; }
        
        /// <summary>
        /// Gets or sets the TURN server password
        /// </summary>
        public string TurnPassword { get; set; }
        
        /// <summary>
        /// Creates a new relay connection with default values
        /// </summary>
        public RelayConnection()
        {
            Status = ConnectionStatus.Disconnected;
            TryDirectConnection = true;
            LastHeartbeat = DateTime.MinValue;
            StunServer = "stun:stun.l.google.com:19302";  // Default STUN server
        }
    }
}
