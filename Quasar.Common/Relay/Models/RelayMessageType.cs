namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Defines the types of relay messages
    /// </summary>
    public enum RelayMessageType
    {
        /// <summary>
        /// Register with the relay server
        /// </summary>
        Register = 0,
        
        /// <summary>
        /// Registration response from relay server
        /// </summary>
        RegisterResponse = 1,
        
        /// <summary>
        /// Connect to another device
        /// </summary>
        Connect = 2,
        
        /// <summary>
        /// Connection response
        /// </summary>
        ConnectResponse = 3,
        
        /// <summary>
        /// Connection request from another device
        /// </summary>
        ConnectionRequest = 4,
        
        /// <summary>
        /// WebRTC offer message
        /// </summary>
        Offer = 5,
        
        /// <summary>
        /// WebRTC answer message
        /// </summary>
        Answer = 6,
        
        /// <summary>
        /// WebRTC ICE candidate message
        /// </summary>
        IceCandidate = 7,
        
        /// <summary>
        /// Heartbeat message to keep connection alive
        /// </summary>
        Heartbeat = 8,
        
        /// <summary>
        /// Heartbeat acknowledgment
        /// </summary>
        HeartbeatAcknowledge = 9,
        
        /// <summary>
        /// Error message
        /// </summary>
        Error = 10,
        
        /// <summary>
        /// Welcome message from server on connection
        /// </summary>
        Welcome = 11
    }
}
