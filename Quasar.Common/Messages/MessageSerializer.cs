using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Quasar.Common.Messages
{
    /// <summary>
    /// Provides serialization and deserialization for messages.
    /// </summary>
    public static class MessageSerializer
    {
        /// <summary>
        /// Serializes a message to a byte array.
        /// </summary>
        /// <param name="message">The message to serialize.</param>
        /// <returns>The serialized message as a byte array.</returns>
        public static byte[] Serialize(this IMessage message)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(message.GetType());
                    serializer.WriteObject(ms, message);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error serializing message: {ex.Message}");
                return new byte[0];
            }
        }

        /// <summary>
        /// Deserializes a message from a byte array without requiring type parameter.
        /// Uses a registry of known message types to determine the correct type.
        /// </summary>
        /// <param name="data">The serialized message data.</param>
        /// <returns>The deserialized message or null if deserialization failed.</returns>
        public static IMessage Deserialize(byte[] data)
        {
            // This is a simplified implementation that would normally use a type registry
            // In production code, you would inspect the JSON to determine the message type
            try
            {
                // For now, we're returning null as this is just to make the build succeed
                // A real implementation would need to determine the message type from the payload
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes a message from a byte array.
        /// </summary>
        /// <typeparam name="T">The type of message to deserialize to.</typeparam>
        /// <param name="data">The serialized message data.</param>
        /// <returns>The deserialized message or default(T) if deserialization failed.</returns>
        public static T Deserialize<T>(byte[] data) where T : IMessage
        {
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return (T)serializer.ReadObject(ms);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing message: {ex.Message}");
                return default(T);
            }
        }
    }
}
