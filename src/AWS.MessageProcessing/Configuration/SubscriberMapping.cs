using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.MessageProcessing.Configuration
{
    /// <summary>
    /// Maps the IMessageHandler to the type of message being processed.
    /// </summary>
    public class SubscriberMapping
    {
        /// <summary>
        /// The IMessageHandler that will process the message.
        /// </summary>
        public Type HandlerType { get; }

        /// <summary>
        /// The .NET type used as the container for the message data.
        /// </summary>
        public Type MessageType { get; }

        /// <summary>
        /// The identifier used as the indicator in the incoming message that maps to the HandlerType. If this
        /// is not set then the FullName of type specified in the MessageType is used.
        /// </summary>
        public string MessageTypeIdentifier { get; }

        /// <summary>
        /// Constructs an instance of HandlerMapping
        /// </summary>
        /// <param name="handlerType"></param>
        /// <param name="messageType"></param>
        /// <param name="messageTypeIdentifier"></param>
        public SubscriberMapping(Type handlerType, Type messageType, string? messageTypeIdentifier = null)
        {
            this.HandlerType = handlerType;
            this.MessageType = messageType;

            if(messageTypeIdentifier != null)
            {
                this.MessageTypeIdentifier = messageTypeIdentifier;
            }
            else
            {
                this.MessageTypeIdentifier = messageType.FullName!;
            }
        }
    }
}
