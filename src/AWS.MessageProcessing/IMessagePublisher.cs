using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AWS.MessageProcessing.Configuration;
using AWS.MessageProcessing.Serialization;

using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWS.MessageProcessing
{
    /// <summary>
    /// An interface that application developers inject into their application to handle publishing messages.
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// Publish the message to the registered publisher for the message type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task PublishAsync<T>(T message, CancellationToken token = default(CancellationToken));
    }

    public class DefaultMessagePublisher : IMessagePublisher
    {
        IServiceProvider _serviceProvider;
        SerializationUtilties _serializationUtilties;
        ILogger<IMessagePublisher> _logger;
        IMessagingConfiguration _messageConfiguration;

        public DefaultMessagePublisher(IServiceProvider serviceProvider, ILogger<IMessagePublisher> logger, IMessagingConfiguration messagingConfiguration)
        {
            _serviceProvider = serviceProvider;
            _serializationUtilties = (SerializationUtilties)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(SerializationUtilties));
            _logger = logger;
            _messageConfiguration = messagingConfiguration;
        }

        public async Task PublishAsync<T>(T message, CancellationToken token = default(CancellationToken))
        {
            var mapping = _messageConfiguration.PublishingMappings?.FirstOrDefault(x => x.MessageType == typeof(T));
            if(mapping == null)
            {
                throw new MissingPublisherMappingException($"Failed to find message mapping for message type {typeof(T).FullName}");
            }    

            switch(mapping.PublishTargetType)
            {
                case PublisherMapping.TargetType.SQS:
                    await PublishSQSAsync<T>(mapping, message, token);
                    break;
                case PublisherMapping.TargetType.SNS:
                    await PublishSNSAsync<T>(mapping, message, token);
                    break;
                case PublisherMapping.TargetType.EventBridge:
                    await PublishEventBridgeAsync<T>(mapping, message, token);
                    break;

            }
        }

        private async Task PublishSQSAsync<T>(PublisherMapping mapping, T message, CancellationToken token = default(CancellationToken))
        {
            var messageBody = _serializationUtilties.Serialize(mapping.MessageTypeIdentifier, message!);


            var client = _serviceProvider.GetRequiredService<IAmazonSQS>();
            var request = new SendMessageRequest
            {
                QueueUrl = mapping.PublishTargetId,
                MessageBody = messageBody
            };

            await client.SendMessageAsync(request);
        }

        private Task PublishSNSAsync<T>(PublisherMapping mapping, T message, CancellationToken token = default(CancellationToken))
        {
            var client = _serviceProvider.GetService<IAmazonSimpleNotificationService>();

            throw new NotImplementedException();
        }

        private Task PublishEventBridgeAsync<T>(PublisherMapping mapping, T message, CancellationToken token = default(CancellationToken))
        {
            var client = _serviceProvider.GetService<IAmazonEventBridge>();

            throw new NotImplementedException();
        }
    }
}
    