using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AWS.MessageProcessing.Serialization
{
    /// <summary>
    /// Used to serialize and deserilize application messages to .NET types.
    /// </summary>
    public interface IMessageSerialization
    {
        /// <summary>
        /// Convert the .NET message object into a string.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        string Serialize(object message);

        /// <summary>
        /// Used to convert the raw string message into the .NET type.
        /// </summary>
        /// <param name="messageData"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        object Deserialize(string messageData, Type returnType);

        /// <summary>
        /// Used to convert the raw string message into the .NET type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageData"></param>
        /// <returns></returns>
        T Deserialize<T>(string messageData)
        {
            return (T)Deserialize(messageData, typeof(T));
        }
    }

    /// <summary>
    /// Default implementation of IMessageSerialization that uses System.Text.Json to serialize and deserialize messages.
    /// </summary>
    public class DefaultMessageSerialization : IMessageSerialization
    {
        /// <inheritdoc/>
        public string Serialize(object message)
        {
            return JsonSerializer.Serialize(message);
        }

        /// <inheritdoc/>
        public object Deserialize(string messageData, Type returnType)
        {
            return JsonSerializer.Deserialize(messageData, returnType);
        }
    }
}
