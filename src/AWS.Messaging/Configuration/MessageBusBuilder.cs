// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Serialization;
using AWS.Messaging.Services;
using AWS.Messaging.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AWS.Messaging.Publishers.SQS;
using AWS.Messaging.Publishers.SNS;
using AWS.Messaging.Publishers.EventBridge;

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
    public IMessageBusBuilder AddSQSPoller(string queueUrl, Action<SQSMessagePollerOptions>? options = null)
    {
        // Create the user-provided options class
        var sqsMessagePollerOptions = new SQSMessagePollerOptions();

        if (options != null)
        {
            options.Invoke(sqsMessagePollerOptions);
        }

        sqsMessagePollerOptions.Validate();

        // Copy that to our internal options class
        var sqsMessagePollerConfiguration = new SQSMessagePollerConfiguration(queueUrl)
        {
            MaxNumberOfConcurrentMessages = sqsMessagePollerOptions.MaxNumberOfConcurrentMessages,
            VisibilityTimeout = sqsMessagePollerOptions.VisibilityTimeout,
            WaitTimeSeconds = sqsMessagePollerOptions.WaitTimeSeconds
        };

        _messageConfiguration.MessagePollerConfigurations.Add(sqsMessagePollerConfiguration);
        return this;
    }

    /// <inheritdoc/>
    public IMessageBusBuilder ConfigureSerializationOptions(Action<SerializationOptions> options)
    {
        options(_messageConfiguration.SerializationOptions);
        return this;
    }

    /// <inheritdoc/>
    public IMessageBusBuilder AddSerializationCallback(ISerializationCallback serializationCallback)
    {
        _messageConfiguration.SerializationCallbacks.Add(serializationCallback);
        return this;
    }

    internal void Build(IServiceCollection services)
    {
        services.AddSingleton<IMessageConfiguration>(_messageConfiguration);
        services.TryAddSingleton<IMessageSerializer, MessageSerializer>();
        services.TryAddSingleton<IEnvelopeSerializer, EnvelopeSerializer>();
        services.TryAddSingleton<IDateTimeHandler, DateTimeHandler>();
        services.TryAddSingleton<IMessageIdGenerator, MessageIdGenerator>();

        if (_messageConfiguration.PublisherMappings.Any())
        {
            services.AddSingleton<IMessagePublisher, MessageRoutingPublisher>();

            if (_messageConfiguration.PublisherMappings.Any(x => x.PublishTargetType == PublisherTargetType.SQS_PUBLISHER))
            {
                services.TryAddAWSService<Amazon.SQS.IAmazonSQS>();
                services.TryAddSingleton<ISQSPublisher, SQSPublisher>();
            }
            if (_messageConfiguration.PublisherMappings.Any(x => x.PublishTargetType == PublisherTargetType.SNS_PUBLISHER))
            {
                services.TryAddAWSService<Amazon.SimpleNotificationService.IAmazonSimpleNotificationService>();
                services.TryAddSingleton<ISNSPublisher, SNSPublisher>();
            }
            if (_messageConfiguration.PublisherMappings.Any(x => x.PublishTargetType == PublisherTargetType.EVENTBRIDGE_PUBLISHER))
            {
                services.TryAddAWSService<Amazon.EventBridge.IAmazonEventBridge>();
                services.TryAddSingleton<IEventBridgePublisher, EventBridgePublisher>();
            }
        }

        if (_messageConfiguration.SubscriberMappings.Any())
        {
            foreach (var subscriberMapping in _messageConfiguration.SubscriberMappings)
            {
                services.AddSingleton(subscriberMapping.HandlerType);
            }
        }

        if (_messageConfiguration.MessagePollerConfigurations.Any())
        {
            services.AddHostedService<MessagePumpService>();
            services.TryAddSingleton<HandlerInvoker>();
            services.TryAddSingleton<IMessagePollerFactory, DefaultMessagePollerFactory>();
            services.TryAddSingleton<IMessageManagerFactory, DefaultMessageManagerFactory>();

            if (_messageConfiguration.MessagePollerConfigurations.OfType<SQSMessagePollerConfiguration>().Any())
            {
                services.TryAddAWSService<Amazon.SQS.IAmazonSQS>();
            }
        }
    }
}
