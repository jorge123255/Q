using System;
using System.Runtime.Serialization;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents a WebRTC signaling message for relay communication
    /// </summary>
    [DataContract]
    public class SignalingMessage
    {
        /// <summary>
        /// The type of signaling message
        /// </summary>
        [DataMember]
        public SignalingType Type { get; set; }
        
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
        /// The message content (SDP or ICE candidate data)
        /// </summary>
        [DataMember]
        public string Data { get; set; }
        
        /// <summary>
        /// The timestamp of the message
        /// </summary>
        [DataMember]
        public long Timestamp { get; set; }

        public SignalingMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
    
    /// <summary>
    /// Types of WebRTC signaling messages
    /// </summary>
    public enum SignalingType
    {
        /// <summary>
        /// Session Description Protocol offer message
        /// </summary>
        Offer,
        
        /// <summary>
        /// Session Description Protocol answer message
        /// </summary>
        Answer,
        
        /// <summary>
        /// ICE Candidate message
        /// </summary>
        IceCandidate
    }
}
