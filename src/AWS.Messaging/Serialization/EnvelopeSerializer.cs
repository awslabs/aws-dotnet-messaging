// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Serialization;

/// <summary>
/// The default implementation of <see cref="IEnvelopeSerializer"/> used by the framework.
/// </summary>
internal class EnvelopeSerializer : IEnvelopeSerializer
{
    private const string CLOUD_EVENT_SPEC_VERSION = "1.0";

    private readonly IMessageConfiguration _messageConfiguration;
    private readonly IMessageSerializer _messageSerializer;
    private readonly IDateTimeHandler _dateTimeHandler;
    private readonly IMessageIdGenerator _messageIdGenerator;
    private readonly ILogger<EnvelopeSerializer> _logger;

    public EnvelopeSerializer(ILogger<EnvelopeSerializer> logger, IMessageConfiguration messageConfiguration, IMessageSerializer messageSerializer, IDateTimeHandler dateTimeHandler, IMessageIdGenerator messageIdGenerator)
    {
        _logger = logger;
        _messageConfiguration = messageConfiguration;
        _messageSerializer = messageSerializer;
        _dateTimeHandler = dateTimeHandler;
        _messageIdGenerator = messageIdGenerator;
    }

    // <inheritdoc/>
    public async ValueTask<MessageEnvelope<T>> CreateEnvelopeAsync<T>(T message)
    {
        var messageId = await _messageIdGenerator.GenerateIdAsync();
        var timeStamp = _dateTimeHandler.GetUtcNow();
        var source = new Uri("/aws/messaging", UriKind.Relative); // TODO: This is a dummy value. The actual value will come via the publisher configuration (pending implementation).

        var publisherMapping = _messageConfiguration.GetPublisherMapping(typeof(T));
        if (publisherMapping is null)
        {
            _logger.LogError("Failed to create a message envelope because a valid publisher mapping for message type '{0}' does not exist.", typeof(T));
            throw new FailedToCreateMessageEnvelopeException($"Failed to create a message envelope because a valid publisher mapping for message type '{typeof(T)}' does not exist.");
        }

        return new MessageEnvelope<T>
        {
            Id = messageId,
            Source = source,
            Version = CLOUD_EVENT_SPEC_VERSION,
            MessageTypeIdentifier = publisherMapping.MessageTypeIdentifier,
            TimeStamp = timeStamp,
            Message = message
        };
    }

    /// <summary>
    /// Serializes the <see cref="MessageEnvelope{T}"/> into a raw string representing a JSON blob
    /// </summary>
    /// <typeparam name="T">The .NET type of the uderlying application message held by <see cref="MessageEnvelope{T}.Message"/></typeparam>
    /// <param name="envelope">The <see cref="MessageEnvelope{T}"/> instance that will be serialized</param>
    /// <returns></returns>
    public string Serialize<T>(MessageEnvelope<T> envelope)
    {
        try
        {
            var message = envelope.Message ?? throw new ArgumentNullException("The underlying application message cannot be null");

            // This blob serves as an intermediate data container because the underlying application message
            // must be serialized seperately as the _messageSerializer can have a user injected implementation.
            var blob = new JsonObject
            {
                ["id"] = envelope.Id,
                ["source"] = envelope.Source?.ToString(),
                ["specversion"] = envelope.Version,
                ["type"] = envelope.MessageTypeIdentifier,
                ["time"] = envelope.TimeStamp,
                ["data"] = _messageSerializer.Serialize(envelope.Message)
            };

            var jsonString = blob.ToJsonString();
            _logger.LogTrace("Serialized the MessageEnvelope object as the following raw string:\n{jsonString}", jsonString);
            return jsonString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize the MessageEnvelope into a JSON string");
            throw new FailedToSerializeMessageEnvelopeException("Failed to serialize the MessageEnvelope into a JSON string", ex);
        }
    }

    // <inheritdoc/>
    public ConvertToEnvelopeResult ConvertToEnvelope(Message sqsMessage)
    {
        try
        {
            var messageEnvelopeConfiguration = GetMessageEnvelopeConfiguration(sqsMessage);
            var intermediateEnvelope = JsonSerializer.Deserialize<MessageEnvelope<string>>(messageEnvelopeConfiguration.MessageEnvelopeBody)!;
            ValidateMessageEnvelope(intermediateEnvelope);
            var messageTypeIdentifier = intermediateEnvelope.MessageTypeIdentifier;
            var subscriberMapping = _messageConfiguration.GetSubscriberMapping(messageTypeIdentifier);
            if (subscriberMapping is null)
            {
                _logger.LogError("{messageConfiguration} does not have a valid subscriber mapping for message ID '{messageTypeIdentifier}'", nameof(_messageConfiguration), messageTypeIdentifier);
                throw new InvalidDataException($"{nameof(_messageConfiguration)} does not have a valid subscriber mapping for {nameof(messageTypeIdentifier)} '{messageTypeIdentifier}'");
            }

            var messageType = subscriberMapping.MessageType;
            var message = _messageSerializer.Deserialize(intermediateEnvelope.Message, messageType);
            var messageEnvelopeType = typeof(MessageEnvelope<>).MakeGenericType(messageType);

            if (Activator.CreateInstance(messageEnvelopeType) is not MessageEnvelope finalMessageEnvelope)
            {
                _logger.LogError("Failed to create a messageEnvelope of type '{messageEnvelopeType}'", messageEnvelopeType.FullName);
                throw new InvalidOperationException($"Failed to create a {nameof(MessageEnvelope)} of type '{messageEnvelopeType.FullName}'");
            }

            finalMessageEnvelope.Id = intermediateEnvelope.Id;
            finalMessageEnvelope.Source = intermediateEnvelope.Source;
            finalMessageEnvelope.Version = intermediateEnvelope.Version;
            finalMessageEnvelope.MessageTypeIdentifier = intermediateEnvelope.MessageTypeIdentifier;
            finalMessageEnvelope.TimeStamp = intermediateEnvelope.TimeStamp;
            finalMessageEnvelope.Metadata = intermediateEnvelope.Metadata;
            finalMessageEnvelope.SQSMetadata = messageEnvelopeConfiguration.SQSMetadata;
            finalMessageEnvelope.SNSMetadata = messageEnvelopeConfiguration.SNSMetadata;
            finalMessageEnvelope.SetMessage(message);

            var result = new ConvertToEnvelopeResult(finalMessageEnvelope, subscriberMapping);

            _logger.LogTrace("Created a generic {messageEnvelopeName} of type '{messageEnvelopeType}'", nameof(MessageEnvelope), result.Envelope.GetType());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create a {messageEnvelopeName}", nameof(MessageEnvelope));
            throw new FailedToCreateMessageEnvelopeException($"Failed to create {nameof(MessageEnvelope)}", ex);
        }
    }

    private void ValidateMessageEnvelope<T>(MessageEnvelope<T>? messageEnvelope)
    {
        if (messageEnvelope is null)
        {
            _logger.LogError("{messageEnvelope} cannot be null", nameof(messageEnvelope));
            throw new InvalidDataException($"{nameof(messageEnvelope)} cannot be null");
        }

        var strBuilder = new StringBuilder();

        if (string.IsNullOrEmpty(messageEnvelope.Id))
            strBuilder.Append($"{nameof(messageEnvelope.Id)} cannot be null or empty.{Environment.NewLine}");

        if (messageEnvelope.Source is null)
            strBuilder.Append($"{nameof(messageEnvelope.Source)} cannot be null.{Environment.NewLine}");

        if (string.IsNullOrEmpty(messageEnvelope.Version))
            strBuilder.Append($"{nameof(messageEnvelope.Version)} cannot be null or empty.{Environment.NewLine}");

        if (string.IsNullOrEmpty(messageEnvelope.MessageTypeIdentifier))
            strBuilder.Append($"{nameof(messageEnvelope.MessageTypeIdentifier)} cannot be null or empty.{Environment.NewLine}");

        if (messageEnvelope.TimeStamp == DateTimeOffset.MinValue)
            strBuilder.Append($"{nameof(messageEnvelope.TimeStamp)} is not set.");

        if (messageEnvelope.Message is null)
            strBuilder.Append($"{nameof(messageEnvelope.Message)} cannot be null.{Environment.NewLine}");

        var validationFailures = strBuilder.ToString();
        if (!string.IsNullOrEmpty(validationFailures))
        {
            _logger.LogError("MessageEnvelope instance is not valid{newline}{errorMessage}", Environment.NewLine, validationFailures);
            throw new InvalidDataException($"MessageEnvelope instance is not valid{Environment.NewLine}{validationFailures}");
        }
    }

    private MessageEnvelopeConfiguration GetMessageEnvelopeConfiguration(Message sqsMessage)
    {
        var messageEnvelopeBody = sqsMessage.Body;
        var sqsMetadata = new SQSMetadata();
        var snsMetadata = new SNSMetadata();

        using (var document = JsonDocument.Parse(sqsMessage.Body))
        {
            var root = document.RootElement;
            // Check if the SQS message body contains an outer envelope injected by SNS.
            if (root.TryGetProperty("TopicArn", out var topicArn) && root.TryGetProperty("Type", out var messageType))
            {
                var topicArnStr = topicArn.GetString() ?? string.Empty;
                var messageTypeStr = messageType.GetString() ?? string.Empty;
                if (topicArnStr.Contains("sns") && string.Equals(messageTypeStr, "Notification"))
                {
                    // Retrieve the inner message envelope
                    messageEnvelopeBody = root.GetProperty("Message").GetString();
                    if (string.IsNullOrEmpty(messageEnvelopeBody))
                    {
                        throw new FailedToCreateMessageEnvelopeConfigurationException("The SNS message envelope does not contain a valid message property.");
                    }

                    // Retrieve SNS message attributes
                    if (root.TryGetProperty("MessageAttributes", out var messageAttributes))
                    {
                        snsMetadata.MessageAttributes = messageAttributes.Deserialize<Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>>()
                        ?? new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>();
                    }
                }
            }
        }

        // Retrieve SQS metdata
        sqsMetadata.MessageAttributes = sqsMessage.MessageAttributes;
        sqsMetadata.ReceiptHandle = sqsMessage.ReceiptHandle;
        if (sqsMessage.Attributes.ContainsKey("MessageGroupId"))
        {
            sqsMetadata.MessageGroupId = sqsMessage.Attributes["MessageGroupId"];
        }
        if (sqsMessage.Attributes.ContainsKey("MessageDeduplicationId"))
        {
            sqsMetadata.MessageDeduplicationId = sqsMessage.Attributes["MessageDeduplicationId"];
        }

        return new MessageEnvelopeConfiguration(messageEnvelopeBody, sqsMetadata, snsMetadata);
    }
}
