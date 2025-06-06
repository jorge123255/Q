namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents the status of a relay connection
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Not connected to the relay server
        /// </summary>
        Disconnected,
        
        /// <summary>
        /// Connected to the relay server but not authenticated
        /// </summary>
        Connected,
        
        /// <summary>
        /// Fully authenticated with the relay server
        /// </summary>
        Authenticated,
        
        /// <summary>
        /// Connection to the relay server failed
        /// </summary>
        Error
    }
}
