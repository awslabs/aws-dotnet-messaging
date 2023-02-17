// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Publishers;

/// <summary>
/// The SNS message publisher allows publishing messages to AWS Simple Notification Service.
/// </summary>
internal class SNSPublisher : IMessagePublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly ILogger<IMessagePublisher> _logger;
    private readonly IMessageConfiguration _messageConfiguration;
    private readonly IEnvelopeSerializer _envelopeSerializer;

    /// <summary>
    /// Creates an instance of <see cref="SNSPublisher"/>.
    /// </summary>
    public SNSPublisher(
        IAmazonSimpleNotificationService snsClient,
        ILogger<IMessagePublisher> logger,
        IMessageConfiguration messageConfiguration,
        IEnvelopeSerializer envelopeSerializer)
    {
        _snsClient = snsClient;
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
        _logger.LogDebug("Publishing the message of type '{messageType}' using the {publisherType}.", typeof(T), nameof(SQSPublisher));

        if (message == null)
        {
            _logger.LogError("A message of type '{messageType}' has a null value.", typeof(T));
            throw new InvalidMessageException("The message cannot be null.");
        }

        var mapping = _messageConfiguration.GetPublisherMapping(typeof(T));
        if (mapping == null)
        {
            _logger.LogError("Cannot find a configuration for the message of type '{messageType}'.", typeof(T));
            throw new MissingMessageTypeConfigurationException($"The framework is not configured to accept messages of type '{typeof(T).FullName}'.");
        }

        _logger.LogDebug("Creating the message envelope for the message of type '{messageType}'.", typeof(T));
        var messageEnvelope = _envelopeSerializer.ConvertToEnvelope<T>(message);
        var messageBody = _envelopeSerializer.Serialize(messageEnvelope);

        var request = new PublishRequest
        {
            TopicArn = mapping.PublisherConfiguration.PublisherEndpoint,
            Message = messageBody
        };

        _logger.LogDebug("Sending the message of type '{messageType}' to SNS.", typeof(T));
        await _snsClient.PublishAsync(request);
        _logger.LogDebug("The message of type '{messageType}' has been pushed to SNS.", typeof(T));
    }
}
