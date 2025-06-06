using System;
using System.Runtime.Serialization;

namespace Quasar.Common.Messages
{
    /// <summary>
    /// Message sent when disconnecting from the server.
    /// </summary>
    [DataContract]
    public class DisconnectMessage : IMessage
    {
        /// <summary>
        /// The reason for disconnection.
        /// </summary>
        [DataMember(Name = "Reason")]
        public string Reason { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectMessage"/> class.
        /// </summary>
        public DisconnectMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectMessage"/> class using the given disconnection reason.
        /// </summary>
        /// <param name="reason">The reason for disconnection.</param>
        public DisconnectMessage(string reason)
        {
            this.Reason = reason;
        }
    }
}
