using System.Collections.Generic;
using System.Linq;

using AWS.MessageProcessing.MessagePump;
using AWS.MessageProcessing.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.MessageProcessing.Configuration
{
    /// <summary>
    /// Builder class used from the IServiceCollection AddAWSMessageBus extension method. The
    /// builder is used during application initialization to configure the AWS.MessageProcessing framework.
    /// </summary>
    public class MessageBusBuilder
    {
        DefaultMessageConfiguration _configuration = new DefaultMessageConfiguration();

        /// <summary>
        /// Add a subscriber for message processing.
        /// </summary>
        /// <typeparam name="THandler">The IMessageHandler that will subscribed to process message of type TMessage.</typeparam>
        /// <typeparam name="TMessage">The type of messages to subscribe to.</typeparam>
        /// <param name="messageTypeIdentifier">The message type identifier to look for in incoming messages to decide if the message should be sent to the THandle. If this is not set then Fullname of type for TMessage will be used.</param>
        /// <returns></returns>
        public MessageBusBuilder AddSubscriberHandler<THandler, TMessage>(string? messageTypeIdentifier = null)
            where THandler : IMessageHandler<TMessage>
        {
            if(_configuration.HandleMappings == null)
            {
                _configuration.HandleMappings = new List<HandlerMapping>();
            }

            _configuration.HandleMappings.Add(new HandlerMapping(typeof(THandler), typeof(TMessage), messageTypeIdentifier));
            return this;
        }

        /// <summary>
        /// Add an SQS queue to poll for messages to process.
        /// </summary>
        /// <param name="queueUrl"></param>
        /// <returns></returns>
        public MessageBusBuilder AddSQSPoller(string queueUrl)
        {
            if (_configuration.SQSPollerConfigurations == null)
            {
                _configuration.SQSPollerConfigurations = new List<SQSPollerConfiguration>();
            }

            _configuration.SQSPollerConfigurations.Add(new SQSPollerConfiguration(queueUrl));
            return this;
        }

        /// <summary>
        /// The build method is called after all configuration has been made on the builder. This method adds the
        /// required services to the IServiceCollection so the rest of the framework can use the resulting IServiceProvider
        /// for creating objects.
        /// </summary>
        /// <param name="services"></param>
        internal void Build(IServiceCollection services)
        {
            services.AddSingleton<IMessagingConfiguration>(_configuration);
            services.AddSingleton<IMessageSerialization, DefaultMessageSerialization>();
            services.AddSingleton<IEnvelopeSerialization, DefaultEnvelopeSerialization>();
            services.AddHostedService<MessagePumpService>();

            if(_configuration.SQSPollerConfigurations?.Any() == true)
            {
                services.TryAddAWSService<Amazon.SQS.IAmazonSQS>();
            }

            if(_configuration.HandleMappings != null)
            {
                foreach(var handlerMapping in _configuration.HandleMappings)
                {
                    services.AddSingleton(handlerMapping.HandlerType);
                }
            }
        }
    }
}
