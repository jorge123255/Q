using System;
using System.Runtime.Serialization;

namespace Quasar.Common.Messages
{
    /// <summary>
    /// Message containing raw data for relay transmission.
    /// </summary>
    [DataContract]
    public class DataMessage : IMessage
    {
        /// <summary>
        /// The raw data being transmitted.
        /// </summary>
        [DataMember(Name = "Data")]
        public byte[] Data { get; set; }

        /// <summary>
        /// Optional type information about the data.
        /// </summary>
        [DataMember(Name = "Type")]
        public string Type { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMessage"/> class.
        /// </summary>
        public DataMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMessage"/> class with the specified data.
        /// </summary>
        /// <param name="data">The raw data to transmit.</param>
        /// <param name="type">Optional type information about the data.</param>
        public DataMessage(byte[] data, string type = null)
        {
            this.Data = data;
            this.Type = type;
        }
    }
}
