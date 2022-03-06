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
        /// This list of mappings from messages to IMessageHandlers
        /// </summary>
        IList<HandlerMapping>? HandleMappings { get; set; }

        /// <summary>
        /// Returns back the handler for the given message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        HandlerMapping? GetHandlerMapping(string messageType);

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
        public IList<HandlerMapping>? HandleMappings { get; set; }

        /// <inheritdoc/>
        public HandlerMapping? GetHandlerMapping(string messageType)
        {
            if (this.HandleMappings == null)
                return null;

            var handleMapping = this.HandleMappings.FirstOrDefault(x => string.Equals(messageType, x.MessageTypeIdentifier, StringComparison.InvariantCulture));
            return handleMapping;
        }

        /// <inheritdoc/>
        public IList<SQSPollerConfiguration>? SQSPollerConfigurations { get; set; }
    }
}
