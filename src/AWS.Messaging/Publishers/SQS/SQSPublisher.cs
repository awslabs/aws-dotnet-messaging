// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Publishers.SNS;
using AWS.Messaging.Serialization;
using AWS.Messaging.Telemetry;
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
    private readonly ITelemetryFactory _telemetryFactory;

    private const string FIFO_SUFFIX = ".fifo";

    /// <summary>
    /// Creates an instance of <see cref="SQSPublisher"/>.
    /// </summary>
    public SQSPublisher(
        IAWSClientProvider awsClientProvider,
        ILogger<IMessagePublisher> logger,
        IMessageConfiguration messageConfiguration,
        IEnvelopeSerializer envelopeSerializer,
        ITelemetryFactory telemetryFactory)
    {
        _sqsClient = awsClientProvider.GetServiceClient<IAmazonSQS>();
        _logger = logger;
        _messageConfiguration = messageConfiguration;
        _envelopeSerializer = envelopeSerializer;
        _telemetryFactory = telemetryFactory;
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
        using (var trace = _telemetryFactory.Trace("Publish to AWS SQS"))
        {
            try
            {
                trace.AddMetadata(TelemetryKeys.ObjectType, typeof(T).FullName!);

                _logger.LogDebug("Publishing the message of type '{MessageType}' using the {PublisherType}.", typeof(T), nameof(SQSPublisher));

                if (message == null)
                {
                    _logger.LogError("A message of type '{MessageType}' has a null value.", typeof(T));
                    throw new InvalidMessageException("The message cannot be null.");
                }

                var queueUrl = GetPublisherEndpoint(trace, typeof(T), sqsOptions);

                _logger.LogDebug("Creating the message envelope for the message of type '{MessageType}'.", typeof(T));
                var messageEnvelope = await _envelopeSerializer.CreateEnvelopeAsync(message);

                trace.AddMetadata(TelemetryKeys.MessageId, messageEnvelope.Id);
                trace.RecordTelemetryContext(messageEnvelope);

                var messageBody = await _envelopeSerializer.SerializeAsync(messageEnvelope);

                var client = _sqsClient;
                if (sqsOptions?.OverrideClient !=  null)
                {
                    // Use the user-provided client
                    client = sqsOptions.OverrideClient;

                    // But still update the user agent to match the built-in client
                    if (client is AmazonServiceClient)
                    {
                        ((AmazonServiceClient)client).BeforeRequestEvent += AWSClientProvider.AWSServiceClient_BeforeServiceRequest;
                    }
                }

                _logger.LogDebug("Sending the message of type '{MessageType}' to SQS. Publisher Endpoint: {Endpoint}", typeof(T), queueUrl);
                var sendMessageRequest = CreateSendMessageRequest(queueUrl, messageBody, sqsOptions);
                await client.SendMessageAsync(sendMessageRequest, token);
                _logger.LogDebug("The message of type '{MessageType}' has been pushed to SQS.", typeof(T));
            }
            catch (Exception ex)
            {
                trace.AddException(ex);
                throw;
            }
        }
    }

    private SendMessageRequest CreateSendMessageRequest(string queueUrl, string messageBody, SQSOptions? sqsOptions)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody,
        };

        if (queueUrl.EndsWith(FIFO_SUFFIX) && string.IsNullOrEmpty(sqsOptions?.MessageGroupId))
        {
            var errorMessage =
                $"You are attempting to publish to a FIFO SQS queue but the request does not include a message group ID. " +
                $"Please use {nameof(ISQSPublisher)} from the service collection to publish to FIFO queues. " +
                $"It exposes a {nameof(PublishAsync)} method that accepts {nameof(SQSOptions)} as a parameter. " +
                $"A message group ID must be specified via {nameof(SQSOptions.MessageGroupId)}. " +
                $"Additionally, {nameof(SQSOptions.MessageDeduplicationId)} must also be specified if content based de-duplication is not enabled on the queue.";

            _logger.LogError(errorMessage);
            throw new InvalidFifoPublishingRequestException(errorMessage);
        }

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

    private string GetPublisherEndpoint(ITelemetryTrace trace, Type messageType, SQSOptions? sqsOptions)
    {
        var mapping = _messageConfiguration.GetPublisherMapping(messageType);
        if (mapping is null)
        {
            _logger.LogError("Cannot find a configuration for the message of type '{MessageType}'.", messageType.FullName);
            throw new MissingMessageTypeConfigurationException($"The framework is not configured to accept messages of type '{messageType.FullName}'.");
        }
        if (mapping.PublishTargetType != PublisherTargetType.SQS_PUBLISHER)
        {
            _logger.LogError("Messages of type '{MessageType}' are not configured for publishing to SQS.", messageType.FullName);
            throw new MissingMessageTypeConfigurationException($"Messages of type '{messageType.FullName}' are not configured for publishing to SQS.");
        }

        var queueUrl = mapping.PublisherConfiguration.PublisherEndpoint;

        // Check if the queue was overriden on this message-specific publishing options
        if (!string.IsNullOrEmpty(sqsOptions?.QueueUrl))
        {
            queueUrl = sqsOptions.QueueUrl;
        }

        if (string.IsNullOrEmpty(queueUrl))
        {
            _logger.LogError("Unable to determine a destination queue for message of type '{MessageType}'.", messageType.FullName);
            throw new InvalidPublisherEndpointException($"Unable to determine a destination queue for message of type '{messageType.FullName}'.");
        }

        trace.AddMetadata(TelemetryKeys.MessageType, mapping.MessageTypeIdentifier);
        trace.AddMetadata(TelemetryKeys.QueueUrl, queueUrl);

        return queueUrl;
    }
}
