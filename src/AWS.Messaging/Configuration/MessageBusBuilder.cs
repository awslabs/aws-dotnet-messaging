// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.Configuration;

/// <summary>
/// This <see cref="MessageBusBuilder"/> is used to configure the AWS messaging framework, including adding publishers and subscribers.
/// </summary>
public class MessageBusBuilder : IMessageBusBuilder
{
    private readonly MessageConfiguration _messageConfiguration;

    /// <summary>
    /// Creates an instance of <see cref="MessageBusBuilder"/>.
    /// </summary>
    public MessageBusBuilder()
    {
        _messageConfiguration = new MessageConfiguration();
    }

    /// <inheritdoc/>
    public IMessageBusBuilder AddSQSPublisher<TMessage>(string queueUrl, string? messageTypeIdentifier = null)
    {
        var sqsPublisherConfiguration = new SQSPublisherConfiguration(queueUrl);
        return AddPublisher<TMessage>(sqsPublisherConfiguration, PublisherTargetType.SQS_PUBLISHER, messageTypeIdentifier);
    }

    /// <inheritdoc/>
    public IMessageBusBuilder AddSNSPublisher<TMessage>(string topicUrl, string? messageTypeIdentifier = null)
    {
        var snsPublisherConfiguration = new SNSPublisherConfiguration(topicUrl);
        return AddPublisher<TMessage>(snsPublisherConfiguration, PublisherTargetType.SNS_PUBLISHER, messageTypeIdentifier);
    }

    /// <inheritdoc/>
    public IMessageBusBuilder AddEventBridgePublisher<TMessage>(string eventBusUrl, string? messageTypeIdentifier = null)
    {
        var eventBridgePublisherConfiguration = new EventBridgePublisherConfiguration(eventBusUrl);
        return AddPublisher<TMessage>(eventBridgePublisherConfiguration, PublisherTargetType.EVENTBRIDGE_PUBLISHER, messageTypeIdentifier);
    }

    private IMessageBusBuilder AddPublisher<TMessage>(IMessagePublisherConfiguration publisherConfiguration, string publisherType, string? messageTypeIdentifier = null)
    {
        var publisherMapping = new PublisherMapping(typeof(TMessage), publisherConfiguration, publisherType, messageTypeIdentifier);
        _messageConfiguration.PublisherMappings.Add(publisherMapping);
        return this;
    }

    /// <inheritdoc/>
    public IMessageBusBuilder AddMessageHandler<THandler, TMessage>(string? messageTypeIdentifier = null)
        where THandler : IMessageHandler<TMessage>
    {
        var subscriberMapping = new SubscriberMapping(typeof(THandler), typeof(TMessage), messageTypeIdentifier);
        _messageConfiguration.SubscriberMappings.Add(subscriberMapping);
        return this;
    }

    /// <inheritdoc/>
    public IMessageBusBuilder AddSQSPoller(string queueUrl)
    {
        _messageConfiguration.MessagePollerConfigurations.Add(new SQSMessagePollerConfiguration(queueUrl));
        return this;
    }

    internal void Build(IServiceCollection services)
    {
        services.AddSingleton<IMessageConfiguration>(_messageConfiguration);

        if (_messageConfiguration.PublisherMappings.Any())
        {
            services.AddSingleton<IMessagePublisher, MessagePublisher>();

            if (_messageConfiguration.PublisherMappings.Any(x => x.PublishTargetType == PublisherTargetType.SQS_PUBLISHER))
            {
                services.TryAddAWSService<Amazon.SQS.IAmazonSQS>();
            }
            if (_messageConfiguration.PublisherMappings.Any(x => x.PublishTargetType == PublisherTargetType.SNS_PUBLISHER))
            {
                services.TryAddAWSService<Amazon.SimpleNotificationService.IAmazonSimpleNotificationService>();
            }
            if (_messageConfiguration.PublisherMappings.Any(x => x.PublishTargetType == PublisherTargetType.EVENTBRIDGE_PUBLISHER))
            {
                services.TryAddAWSService<Amazon.EventBridge.IAmazonEventBridge>();
            }
        }

        if (_messageConfiguration.SubscriberMappings.Any())
        {
            foreach (var subscriberMapping in _messageConfiguration.SubscriberMappings)
            {
                services.AddSingleton(subscriberMapping.HandlerType);
            }
        }
    }
}
