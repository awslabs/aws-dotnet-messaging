using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AWS.MessageProcessing.Configuration
{
    /// <summary>
    /// The configuration used for messaging.
    /// </summary>
    public interface IMessagingConfiguration
    {
        /// <summary>
        /// The list of mappings from messages to IMessageHandlers
        /// </summary>
        IList<SubscriberMapping>? SubscriberMappings { get; set; }

        /// <summary>
        /// The list of mappings of messages to publishing targets.
        /// </summary>
        IList<PublisherMapping>? PublishingMappings { get; set; }

        /// <summary>
        /// Returns back the handler for the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        SubscriberMapping? GetSubscriberMapping(string messageType);

        /// <summary>
        /// List of configurations for SQS queues to poll.
        /// </summary>
        IList<SQSPollerConfiguration>? SQSPollerConfigurations { get; set; }
    }

    /// <summary>
    /// Default implementation of IMessageConfiguration
    /// </summary>
    public class DefaultMessageConfiguration : IMessagingConfiguration
    {
        /// <inheritdoc/>
        public IList<SubscriberMapping>? SubscriberMappings { get; set; }

        public IList<PublisherMapping>? PublishingMappings { get; set; }

        /// <inheritdoc/>
        public SubscriberMapping? GetSubscriberMapping(string messageType)
        {
            if (this.SubscriberMappings == null)
                return null;

            var handleMapping = this.SubscriberMappings.FirstOrDefault(x => string.Equals(messageType, x.MessageTypeIdentifier, StringComparison.InvariantCulture));
            return handleMapping;
        }

        /// <inheritdoc/>
        public IList<SQSPollerConfiguration>? SQSPollerConfigurations { get; set; }
    }
}
