using Newtonsoft.Json;
using System;

namespace Quasar.Common.Relay.Models
{
    /// <summary>
    /// Represents message types for relay communication
    /// </summary>
    public enum RelayMessageType
    {
        Register,
        RegisterResponse,
        Connect,
        ConnectResponse,
        ConnectionRequest,
        Offer,
        Answer,
        IceCandidate,
        Heartbeat,
        HeartbeatAcknowledge,
        Error
    }

    /// <summary>
    /// Base class for all relay messages
    /// </summary>
    public class RelayMessage
    {
        /// <summary>
        /// Gets or sets the message type
        /// </summary>
        [JsonProperty("type")]
        public RelayMessageType Type { get; set; }

        /// <summary>
        /// Converts the message to JSON
        /// </summary>
        /// <returns>JSON string representation of the message</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        /// <summary>
        /// Parses a JSON string into a relay message
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <returns>Parsed relay message</returns>
        public static RelayMessage FromJson(string json)
        {
            // First deserialize to a base message to get the type
            var baseMessage = JsonConvert.DeserializeObject<RelayMessage>(json);
            
            // Then deserialize to the specific message type
            switch (baseMessage.Type)
            {
                case RelayMessageType.Register:
                    return JsonConvert.DeserializeObject<RegisterMessage>(json);
                case RelayMessageType.RegisterResponse:
                    return JsonConvert.DeserializeObject<RegisterResponseMessage>(json);
                case RelayMessageType.Connect:
                    return JsonConvert.DeserializeObject<ConnectMessage>(json);
                case RelayMessageType.ConnectResponse:
                    return JsonConvert.DeserializeObject<ConnectResponseMessage>(json);
                case RelayMessageType.ConnectionRequest:
                    return JsonConvert.DeserializeObject<ConnectionRequestMessage>(json);
                case RelayMessageType.Offer:
                case RelayMessageType.Answer:
                case RelayMessageType.IceCandidate:
                    return JsonConvert.DeserializeObject<SignalingMessage>(json);
                case RelayMessageType.Heartbeat:
                    return JsonConvert.DeserializeObject<HeartbeatMessage>(json);
                case RelayMessageType.HeartbeatAcknowledge:
                    return JsonConvert.DeserializeObject<HeartbeatAcknowledgeMessage>(json);
                case RelayMessageType.Error:
                    return JsonConvert.DeserializeObject<ErrorMessage>(json);
                default:
                    return baseMessage;
            }
        }
    }

    /// <summary>
    /// Message for registering a device with the relay server
    /// </summary>
    public class RegisterMessage : RelayMessage
    {
        public RegisterMessage()
        {
            Type = RelayMessageType.Register;
        }

        /// <summary>
        /// Gets or sets the device ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the device password
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the device type (client or server)
        /// </summary>
        [JsonProperty("type")]
        public string DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the device name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Response message for device registration
    /// </summary>
    public class RegisterResponseMessage : RelayMessage
    {
        public RegisterResponseMessage()
        {
            Type = RelayMessageType.RegisterResponse;
        }

        /// <summary>
        /// Gets or sets whether the registration was successful
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the device ID (returned by server)
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the error message if registration failed
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }

    /// <summary>
    /// Message for requesting a connection to another device
    /// </summary>
    public class ConnectMessage : RelayMessage
    {
        public ConnectMessage()
        {
            Type = RelayMessageType.Connect;
        }

        /// <summary>
        /// Gets or sets the target device ID to connect to
        /// </summary>
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        /// <summary>
        /// Gets or sets the session ID (if reconnecting to an existing session)
        /// </summary>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Response message for connection requests
    /// </summary>
    public class ConnectResponseMessage : RelayMessage
    {
        public ConnectResponseMessage()
        {
            Type = RelayMessageType.ConnectResponse;
        }

        /// <summary>
        /// Gets or sets whether the connection request was successful
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the session ID for this connection
        /// </summary>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the error message if the connection request failed
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }

    /// <summary>
    /// Message for incoming connection requests
    /// </summary>
    public class ConnectionRequestMessage : RelayMessage
    {
        public ConnectionRequestMessage()
        {
            Type = RelayMessageType.ConnectionRequest;
        }

        /// <summary>
        /// Gets or sets the ID of the device requesting the connection
        /// </summary>
        [JsonProperty("fromId")]
        public string FromId { get; set; }

        /// <summary>
        /// Gets or sets the session ID for this connection
        /// </summary>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }

    /// <summary>
    /// Message for WebRTC signaling (offer, answer, ICE candidates)
    /// </summary>
    public class SignalingMessage : RelayMessage
    {
        /// <summary>
        /// Gets or sets the target device ID
        /// </summary>
        [JsonProperty("targetId")]
        public string TargetId { get; set; }

        /// <summary>
        /// Gets or sets the source device ID (filled by server)
        /// </summary>
        [JsonProperty("fromId")]
        public string FromId { get; set; }

        /// <summary>
        /// Gets or sets the session ID for this connection
        /// </summary>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the SDP for offer/answer messages
        /// </summary>
        [JsonProperty("sdp")]
        public string Sdp { get; set; }

        /// <summary>
        /// Gets or sets the ICE candidate
        /// </summary>
        [JsonProperty("candidate")]
        public string Candidate { get; set; }

        /// <summary>
        /// Gets or sets the SDP mid attribute for ICE candidates
        /// </summary>
        [JsonProperty("sdpMid")]
        public string SdpMid { get; set; }

        /// <summary>
        /// Gets or sets the SDP m-line index for ICE candidates
        /// </summary>
        [JsonProperty("sdpMLineIndex")]
        public int? SdpMLineIndex { get; set; }
    }

    /// <summary>
    /// Message for sending heartbeats to keep connections alive
    /// </summary>
    public class HeartbeatMessage : RelayMessage
    {
        public HeartbeatMessage()
        {
            Type = RelayMessageType.Heartbeat;
        }
    }

    /// <summary>
    /// Message acknowledging a heartbeat
    /// </summary>
    public class HeartbeatAcknowledgeMessage : RelayMessage
    {
        public HeartbeatAcknowledgeMessage()
        {
            Type = RelayMessageType.HeartbeatAcknowledge;
        }
    }

    /// <summary>
    /// Message for error responses
    /// </summary>
    public class ErrorMessage : RelayMessage
    {
        public ErrorMessage()
        {
            Type = RelayMessageType.Error;
        }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
