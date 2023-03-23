// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Publishers.SQS;

/// <summary>
/// The SQS message publisher allows publishing messages to AWS SQS.
/// </summary>
internal class SQSPublisher : IMessagePublisher, ISQSPublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<IMessagePublisher> _logger;
    private readonly IMessageConfiguration _messageConfiguration;
    private readonly IEnvelopeSerializer _envelopeSerializer;

    /// <summary>
    /// Creates an instance of <see cref="SQSPublisher"/>.
    /// </summary>
    public SQSPublisher(
        IAmazonSQS sqsClient,
        ILogger<IMessagePublisher> logger,
        IMessageConfiguration messageConfiguration,
        IEnvelopeSerializer envelopeSerializer)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _messageConfiguration = messageConfiguration;
        _envelopeSerializer = envelopeSerializer;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="message">The application message that will be serialized and sent to an SQS queue</param>
    /// <param name="token">The cancellation token used to cancel the request.</param>
    /// <exception cref="InvalidMessageException">If the message is null or invalid.</exception>
    /// <exception cref="MissingMessageTypeConfigurationException">If cannot find the publisher configuration for the message type.</exception>
    public async Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        await PublishAsync(message, null, token);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="message">The application message that will be serialized and sent to an SQS queue</param>
    /// <param name="sqsOptions">Contains additional parameters that can be set while sending a message to an SQS queue</param>
    /// <param name="token">The cancellation token used to cancel the request.</param>
    /// <exception cref="InvalidMessageException">If the message is null or invalid.</exception>
    /// <exception cref="MissingMessageTypeConfigurationException">If cannot find the publisher configuration for the message type.</exception>
    public async Task PublishAsync<T>(T message, SQSOptions? sqsOptions, CancellationToken token = default)
    {
        _logger.LogDebug("Publishing the message of type '{messageType}' using the {publisherType}.", typeof(T), nameof(SQSPublisher));

        if (message == null)
        {
            _logger.LogError("A message of type '{messageType}' has a null value.", typeof(T));
            throw new InvalidMessageException("The message cannot be null.");
        }

        var publisherEndpoint = GetPublisherEndpoint(typeof(T));

        _logger.LogDebug("Creating the message envelope for the message of type '{messageType}'.", typeof(T));
        var messageEnvelope = await _envelopeSerializer.CreateEnvelopeAsync(message);
        var messageBody = _envelopeSerializer.Serialize(messageEnvelope);

        _logger.LogDebug("Sending the message of type '{messageType}' to SQS. Publisher Endpoint: {endpoint}", typeof(T), publisherEndpoint);
        var sendMessageRequest = CreateSendMessageRequest(publisherEndpoint, messageBody, sqsOptions);
        await _sqsClient.SendMessageAsync(sendMessageRequest, token);
        _logger.LogDebug("The message of type '{messageType}' has been pushed to SQS.", typeof(T));
    }

    private SendMessageRequest CreateSendMessageRequest(string queueUrl, string messageBody, SQSOptions? sqsOptions)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody,
        };

        if (sqsOptions is null)
            return request;

        if (!string.IsNullOrEmpty(sqsOptions.MessageDeduplicationId))
            request.MessageDeduplicationId = sqsOptions.MessageDeduplicationId;

        if (!string.IsNullOrEmpty(sqsOptions.MessageGroupId))
            request.MessageGroupId = sqsOptions.MessageGroupId;

        if (sqsOptions.DelaySeconds.HasValue)
            request.DelaySeconds = (int)sqsOptions.DelaySeconds;

        if (sqsOptions.MessageAttributes is not null)
            request.MessageAttributes = sqsOptions.MessageAttributes;

        return request;
    }

    private string GetPublisherEndpoint(Type messageType)
    {
        var mapping = _messageConfiguration.GetPublisherMapping(messageType);
        if (mapping is null)
        {
            _logger.LogError("Cannot find a configuration for the message of type '{messageType}'.", messageType.FullName);
            throw new MissingMessageTypeConfigurationException($"The framework is not configured to accept messages of type '{messageType.FullName}'.");
        }
        if (mapping.PublishTargetType != PublisherTargetType.SQS_PUBLISHER)
        {
            _logger.LogError("Messages of type '{messageType}' are not configured for publishing to SQS.", messageType.FullName);
            throw new MissingMessageTypeConfigurationException($"Messages of type '{messageType.FullName}' are not configured for publishing to SQS.");
        }
        return mapping.PublisherConfiguration.PublisherEndpoint;
    }
}
