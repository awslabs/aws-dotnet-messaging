using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.MessageProcessing.Configuration
{
    public class PublisherMapping
    {
        public enum TargetType { SQS, SNS, EventBridge}

        /// <summary>
        /// The .NET type used as the container for the message data.
        /// </summary>
        public Type MessageType { get; }

        public string PublishTargetId { get; }

        public TargetType PublishTargetType { get; }

        /// <summary>
        /// The identifier used as the indicator in the incoming message that maps to the HandlerType. If this
        /// is not set then the FullName of type specified in the MessageType is used.
        /// </summary>
        public string MessageTypeIdentifier { get; }

        public PublisherMapping(Type messageType, string publishTargetId, TargetType publishTargetType, string? messageTypeIdentifier = null)
        {
            this.MessageType = messageType;
            this.PublishTargetId = publishTargetId;
            this.PublishTargetType = publishTargetType;

            if (messageTypeIdentifier != null)
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
