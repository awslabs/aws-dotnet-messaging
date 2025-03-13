// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;
using AWS.Messaging.Serialization.Parsers;

namespace AWS.Messaging.Serialization;

/// <summary>
/// The default implementation of <see cref="IEnvelopeSerializer"/> used by the framework.
/// </summary>
internal class EnvelopeSerializer : IEnvelopeSerializer
{
    private Uri? MessageSource { get; set; }
    private const string CLOUD_EVENT_SPEC_VERSION = "1.0";

    private readonly IMessageConfiguration _messageConfiguration;
    private readonly IMessageSerializer _messageSerializer;
    private readonly IDateTimeHandler _dateTimeHandler;
    private readonly IMessageIdGenerator _messageIdGenerator;
    private readonly IMessageSourceHandler _messageSourceHandler;
    private readonly ILogger<EnvelopeSerializer> _logger;

    public EnvelopeSerializer(
        ILogger<EnvelopeSerializer> logger,
        IMessageConfiguration messageConfiguration,
        IMessageSerializer messageSerializer,
        IDateTimeHandler dateTimeHandler,
        IMessageIdGenerator messageIdGenerator,
        IMessageSourceHandler messageSourceHandler)
    {
        _logger = logger;
        _messageConfiguration = messageConfiguration;
        _messageSerializer = messageSerializer;
        _dateTimeHandler = dateTimeHandler;
        _messageIdGenerator = messageIdGenerator;
        _messageSourceHandler = messageSourceHandler;
    }

    /// <inheritdoc/>
    public async ValueTask<MessageEnvelope<T>> CreateEnvelopeAsync<T>(T message)
    {
        var messageId = await _messageIdGenerator.GenerateIdAsync();
        var timeStamp = _dateTimeHandler.GetUtcNow();

        var publisherMapping = _messageConfiguration.GetPublisherMapping(typeof(T));
        if (publisherMapping is null)
        {
            _logger.LogError("Failed to create a message envelope because a valid publisher mapping for message type '{MessageType}' does not exist.", typeof(T));
            throw new FailedToCreateMessageEnvelopeException($"Failed to create a message envelope because a valid publisher mapping for message type '{typeof(T)}' does not exist.");
        }

        if (MessageSource is null)
        {
            MessageSource = await _messageSourceHandler.ComputeMessageSource();
        }

        return new MessageEnvelope<T>
        {
            Id = messageId,
            Source = MessageSource,
            Version = CLOUD_EVENT_SPEC_VERSION,
            MessageTypeIdentifier = publisherMapping.MessageTypeIdentifier,
            TimeStamp = timeStamp,
            Message = message
        };
    }

    /// <summary>
    /// Serializes the <see cref="MessageEnvelope{T}"/> into a raw string representing a JSON blob
    /// </summary>
    /// <typeparam name="T">The .NET type of the underlying application message held by <see cref="MessageEnvelope{T}.Message"/></typeparam>
    /// <param name="envelope">The <see cref="MessageEnvelope{T}"/> instance that will be serialized</param>
    public async ValueTask<string> SerializeAsync<T>(MessageEnvelope<T> envelope)
    {
        try
        {
            await InvokePreSerializationCallback(envelope);
            var message = envelope.Message ?? throw new ArgumentNullException("The underlying application message cannot be null");

            // This blob serves as an intermediate data container because the underlying application message
            // must be serialized separately as the _messageSerializer can have a user injected implementation.
            var blob = new JsonObject
            {
                ["id"] = envelope.Id,
                ["source"] = envelope.Source?.ToString(),
                ["specversion"] = envelope.Version,
                ["type"] = envelope.MessageTypeIdentifier,
                ["time"] = envelope.TimeStamp,
                ["data"] = _messageSerializer.Serialize(message)
            };

            // Write any Metadata as top-level keys
            // This may be useful for any extensions defined in
            // https://github.com/cloudevents/spec/tree/main/cloudevents/extensions
            foreach (var key in envelope.Metadata.Keys)
            {
                if (!blob.ContainsKey(key)) // don't overwrite any reserved keys
                {
                    blob[key] = JsonSerializer.SerializeToNode(envelope.Metadata[key]);
                }
            }

            var jsonString = blob.ToJsonString();
            var serializedMessage = await InvokePostSerializationCallback(jsonString);

            if (_messageConfiguration.LogMessageContent)
            {
                _logger.LogTrace("Serialized the MessageEnvelope object as the following raw string:\n{SerializedMessage}", serializedMessage);
            }
            else
            {
                _logger.LogTrace("Serialized the MessageEnvelope object to a raw string");
            }
            return serializedMessage;
        }
        catch (JsonException) when (!_messageConfiguration.LogMessageContent)
        {
            _logger.LogError("Failed to serialize the MessageEnvelope into a raw string");
            throw new FailedToSerializeMessageEnvelopeException("Failed to serialize the MessageEnvelope into a raw string");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize the MessageEnvelope into a raw string");
            throw new FailedToSerializeMessageEnvelopeException("Failed to serialize the MessageEnvelope into a raw string", ex);
        }
    }

    /// <inheritdoc/>
    public async ValueTask<ConvertToEnvelopeResult> ConvertToEnvelopeAsync(Message sqsMessage)
    {
        try
        {
            // Get the raw envelope JSON and metadata from the appropriate wrapper (SNS/EventBridge/SQS)
            var (envelopeJson, metadata) = await ParseOuterWrapper(sqsMessage);

            // Parse just the type field first to get the correct mapping
            var messageType = GetMessageTypeFromEnvelope(envelopeJson);
            var subscriberMapping = GetAndValidateSubscriberMapping(messageType);

            // Create and populate the envelope with the correct type
            var envelope = CreateEnvelopeFromJson(envelopeJson, subscriberMapping.MessageType);

            // Add metadata from outer wrapper
            envelope.SQSMetadata = metadata.SQSMetadata;
            envelope.SNSMetadata = metadata.SNSMetadata;
            envelope.EventBridgeMetadata = metadata.EventBridgeMetadata;

            await InvokePostDeserializationCallback(envelope);
            return new ConvertToEnvelopeResult(envelope, subscriberMapping);
        }
        catch (JsonException) when (!_messageConfiguration.LogMessageContent)
        {
            _logger.LogError("Failed to create a {MessageEnvelopeName}", nameof(MessageEnvelope));
            throw new FailedToCreateMessageEnvelopeException($"Failed to create {nameof(MessageEnvelope)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create a {MessageEnvelopeName}", nameof(MessageEnvelope));
            throw new FailedToCreateMessageEnvelopeException($"Failed to create {nameof(MessageEnvelope)}", ex);
        }
    }

    private MessageEnvelope CreateEnvelopeFromJson(string json, Type messageType)
    {
        // Parse the envelope JSON
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Create envelope of correct type
        var envelopeType = typeof(MessageEnvelope<>).MakeGenericType(messageType);
        var envelope = Activator.CreateInstance(envelopeType) as MessageEnvelope
            ?? throw new InvalidOperationException($"Failed to create envelope of type {envelopeType}");

        // Set the envelope properties
        envelope.Id = root.GetProperty("id").GetString()!;
        envelope.Source = new Uri(root.GetProperty("source").GetString()!, UriKind.RelativeOrAbsolute);
        envelope.Version = root.GetProperty("specversion").GetString()!;
        envelope.MessageTypeIdentifier = root.GetProperty("type").GetString()!;
        envelope.TimeStamp = root.GetProperty("time").GetDateTimeOffset();

        // Handle metadata if present
        if (root.TryGetProperty("metadata", out var metadataElement))
        {
            envelope.Metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadataElement)!;
        }

        // Deserialize the message content using the custom serializer
        var dataContent = root.GetProperty("data").GetString()!;
        var message = _messageSerializer.Deserialize(dataContent, messageType);
        envelope.SetMessage(message);

        ValidateMessageEnvelope(envelope);
        return envelope;
    }

    private static string GetMessageTypeFromEnvelope(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("type").GetString()
            ?? throw new InvalidDataException("Message type identifier not found in envelope");
    }

    private async Task<(string MessageBody, MessageMetadata Metadata)> ParseOuterWrapper(Message sqsMessage)
    {
        sqsMessage.Body = await InvokePreDeserializationCallback(sqsMessage.Body);

        JsonElement rootCopy;
        using (var document = JsonDocument.Parse(sqsMessage.Body))
        {
            rootCopy = document.RootElement.Clone();
        }

        var parsers = new IMessageParser[]
        {
            new SNSMessageParser(),
            new EventBridgeMessageParser(),
            new SQSMessageParser()
        };

        string currentMessageBody = sqsMessage.Body;
        var combinedMetadata = new MessageMetadata();

        // Try all parsers in order
        foreach (var parser in parsers.Where(p => p.CanParse(rootCopy)))
        {
            var (messageBody, metadata) = parser.Parse(rootCopy, sqsMessage);

            // Update the message body if this parser extracted an inner message
            if (!string.IsNullOrEmpty(messageBody))
            {
                currentMessageBody = messageBody;
                // Parse the new message body for the next iteration
                using var newDoc = JsonDocument.Parse(messageBody);
                rootCopy = newDoc.RootElement.Clone();
            }

            // Combine metadata
            if (metadata.SQSMetadata != null) combinedMetadata.SQSMetadata = metadata.SQSMetadata;
            if (metadata.SNSMetadata != null) combinedMetadata.SNSMetadata = metadata.SNSMetadata;
            if (metadata.EventBridgeMetadata != null) combinedMetadata.EventBridgeMetadata = metadata.EventBridgeMetadata;
        }

        return (currentMessageBody, combinedMetadata);
    }

    private void ValidateMessageEnvelope(MessageEnvelope? messageEnvelope)
    {
        if (messageEnvelope is null)
            throw new InvalidDataException($"{nameof(messageEnvelope)} cannot be null");

        var messageProperty = messageEnvelope.GetType().GetProperty("Message");
        var messageValue = messageProperty?.GetValue(messageEnvelope);

        var validations = new[]
        {
            (string.IsNullOrEmpty(messageEnvelope.Id), $"{nameof(messageEnvelope.Id)} cannot be null or empty."),
            (messageEnvelope.Source is null, $"{nameof(messageEnvelope.Source)} cannot be null."),
            (string.IsNullOrEmpty(messageEnvelope.Version), $"{nameof(messageEnvelope.Version)} cannot be null or empty."),
            (string.IsNullOrEmpty(messageEnvelope.MessageTypeIdentifier), $"{nameof(messageEnvelope.MessageTypeIdentifier)} cannot be null or empty."),
            (messageEnvelope.TimeStamp == DateTimeOffset.MinValue, $"{nameof(messageEnvelope.TimeStamp)} is not set."),
            (messageValue is null, "Message cannot be null.")
        };

        var failures = validations
            .Where(v => v.Item1)
            .Select(v => v.Item2)
            .ToList();

        if (failures.Any())
        {
            var message = string.Join(Environment.NewLine, failures);
            _logger.LogError("MessageEnvelope instance is not valid\n{ValidationFailures}", message);
            throw new InvalidDataException($"MessageEnvelope instance is not valid{Environment.NewLine}{message}");
        }
    }

    private SubscriberMapping GetAndValidateSubscriberMapping(string messageTypeIdentifier)
    {
        var subscriberMapping = _messageConfiguration.GetSubscriberMapping(messageTypeIdentifier);
        if (subscriberMapping is null)
        {
            var availableMappings = string.Join(", ",
                _messageConfiguration.SubscriberMappings.Select(m => m.MessageTypeIdentifier));

            _logger.LogError(
                "'{MessageTypeIdentifier}' is not a valid subscriber mapping. Available mappings: {AvailableMappings}",
                messageTypeIdentifier,
                string.IsNullOrEmpty(availableMappings) ? "none" : availableMappings);

            throw new InvalidDataException(
                $"'{messageTypeIdentifier}' is not a valid subscriber mapping. " +
                $"Available mappings: {(string.IsNullOrEmpty(availableMappings) ? "none" : availableMappings)}");
        }
        return subscriberMapping;
    }

    private async ValueTask InvokePreSerializationCallback(MessageEnvelope messageEnvelope)
    {
        foreach (var serializationCallback in _messageConfiguration.SerializationCallbacks)
        {
            await serializationCallback.PreSerializationAsync(messageEnvelope);
        }
    }

    private async ValueTask<string> InvokePostSerializationCallback(string message)
    {
        foreach (var serializationCallback in _messageConfiguration.SerializationCallbacks)
        {
            message = await serializationCallback.PostSerializationAsync(message);
        }
        return message;
    }

    private async ValueTask<string> InvokePreDeserializationCallback(string message)
    {
        foreach (var serializationCallback in _messageConfiguration.SerializationCallbacks)
        {
            message = await serializationCallback.PreDeserializationAsync(message);
        }
        return message;
    }

    private async ValueTask InvokePostDeserializationCallback(MessageEnvelope messageEnvelope)
    {
        foreach (var serializationCallback in _messageConfiguration.SerializationCallbacks)
        {
            await serializationCallback.PostDeserializationAsync(messageEnvelope);
        }
    }
}
