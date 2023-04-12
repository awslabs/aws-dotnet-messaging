// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Publishers.EventBridge;

/// <summary>
/// The EventBridge message publisher allows publishing messages to Amazon EventBridge.
/// </summary>
internal class EventBridgePublisher : IMessagePublisher, IEventBridgePublisher
{
    private readonly IAmazonEventBridge _eventBridgeClient;
    private readonly ILogger<IMessagePublisher> _logger;
    private readonly IMessageConfiguration _messageConfiguration;
    private readonly IEnvelopeSerializer _envelopeSerializer;

    /// <summary>
    /// Creates an instance of <see cref="EventBridgePublisher"/>.
    /// </summary>
    public EventBridgePublisher(
        IAWSClientProvider awsClientProvider,
        ILogger<IMessagePublisher> logger,
        IMessageConfiguration messageConfiguration,
        IEnvelopeSerializer envelopeSerializer)
    {
        _eventBridgeClient = awsClientProvider.GetServiceClient<IAmazonEventBridge>();
        _logger = logger;
        _messageConfiguration = messageConfiguration;
        _envelopeSerializer = envelopeSerializer;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <exception cref="InvalidMessageException">If the message is null or invalid.</exception>
    /// <exception cref="MissingMessageTypeConfigurationException">If cannot find the publisher configuration for the message type.</exception>
    public async Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        await PublishAsync(message, null, token);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="message">The application message that will be serialized and sent to an event bus</param>
    /// <param name="eventBridgeOptions">Contains additional parameters that can be set while sending a message to EventBridge</param>
    /// <param name="token">The cancellation token used to cancel the request.</param>
    /// <exception cref="InvalidMessageException">If the message is null or invalid.</exception>
    /// <exception cref="MissingMessageTypeConfigurationException">If cannot find the publisher configuration for the message type.</exception>
    public async Task PublishAsync<T>(T message, EventBridgeOptions? eventBridgeOptions, CancellationToken token = default)
    {
        _logger.LogDebug("Publishing the message of type '{messageType}' using the {publisherType}.", typeof(T), nameof(EventBridgePublisher));

        if (message == null)
        {
            _logger.LogError("A message of type '{messageType}' has a null value.", typeof(T));
            throw new InvalidMessageException("The message cannot be null.");
        }

        var publisherMapping = GetPublisherMapping(typeof(T));
        var publisherEndpoint = publisherMapping.PublisherConfiguration.PublisherEndpoint;

        _logger.LogDebug("Creating the message envelope for the message of type '{messageType}'.", typeof(T));
        var messageEnvelope = await _envelopeSerializer.CreateEnvelopeAsync(message);
        var messageBody = await _envelopeSerializer.SerializeAsync(messageEnvelope);

        _logger.LogDebug("Sending the message of type '{messageType}' to EventBridge. Publisher Endpoint: {endpoint}", typeof(T), publisherEndpoint);
        var request = CreatePutEventsRequest(publisherMapping, messageEnvelope.Source?.ToString(), messageBody, eventBridgeOptions);
        await _eventBridgeClient.PutEventsAsync(request, token);
        _logger.LogDebug("The message of type '{messageType}' has been pushed to EventBridge.", typeof(T));
    }

    private PutEventsRequest CreatePutEventsRequest(PublisherMapping publisherMapping, string? source, string messageBody, EventBridgeOptions? eventBridgeOptions)
    {
        var publisherEndpoint = publisherMapping.PublisherConfiguration.PublisherEndpoint;
        var publisherConfiguration = (EventBridgePublisherConfiguration)publisherMapping.PublisherConfiguration;

        var requestEntry = new PutEventsRequestEntry
        {
            EventBusName = publisherEndpoint,
            DetailType = publisherMapping.MessageTypeIdentifier,
            Detail = messageBody
        };

        var putEventsRequest = new PutEventsRequest
        {
            EndpointId = publisherConfiguration.EndpointID,
            Entries = new() { requestEntry }
        };

        if (!string.IsNullOrEmpty(eventBridgeOptions?.Source))
            requestEntry.Source = eventBridgeOptions.Source;
        else if(!string.IsNullOrEmpty(source))
            requestEntry.Source = source;

        if (!string.IsNullOrEmpty(eventBridgeOptions?.TraceHeader))
            requestEntry.TraceHeader = eventBridgeOptions.TraceHeader;

        if (eventBridgeOptions != null && eventBridgeOptions.Time != DateTimeOffset.MinValue)
            requestEntry.Time = eventBridgeOptions.Time.DateTime;

        if (!string.IsNullOrEmpty(eventBridgeOptions?.DetailType))
            requestEntry.DetailType = eventBridgeOptions.DetailType;

        if (eventBridgeOptions?.Resources?.Any() ?? false)
            requestEntry.Resources = eventBridgeOptions.Resources;

        return putEventsRequest;
    }

    private PublisherMapping GetPublisherMapping(Type messageType)
    {
        var mapping = _messageConfiguration.GetPublisherMapping(messageType);
        if (mapping is null)
        {
            _logger.LogError("Cannot find a configuration for the message of type '{messageType}'.", messageType.FullName);
            throw new MissingMessageTypeConfigurationException($"The framework is not configured to accept messages of type '{messageType.FullName}'.");
        }
        if (mapping.PublishTargetType != PublisherTargetType.EVENTBRIDGE_PUBLISHER)
        {
            _logger.LogError("Messages of type '{messageType}' are not configured for publishing to EventBridge.", messageType.FullName);
            throw new MissingMessageTypeConfigurationException($"Messages of type '{messageType.FullName}' are not configured for publishing to EventBridge.");
        }
        return mapping;
    }
}
