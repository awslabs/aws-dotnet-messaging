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
            Message = message,
            // DataContentType = "" // TODO
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
                ["time"] = envelope.TimeStamp
            };

            // See https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md#31-handling-of-data for more details.
            SerializeData(message, blob, envelope.DataContentType);

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
            var finalSerializedMessage  = await InvokePostSerializationCallback(jsonString);

            if (_messageConfiguration.LogMessageContent)
            {
                _logger.LogTrace("Serialized the MessageEnvelope object as the following raw string:\n{SerializedMessage}", finalSerializedMessage );
            }
            else
            {
                _logger.LogTrace("Serialized the MessageEnvelope object to a raw string");
            }
            return finalSerializedMessage ;
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

    private void SerializeData<T>(T message, JsonObject blob, string? dataContentType)
    {
        if (message == null)
        {
            throw new ArgumentNullException("The underlying application message cannot be null");
        }

        // Serialize the message
        var serializedMessage = _messageSerializer.Serialize(message);

        // Determine if the serialized message is valid JSON
        // Wed do this because _messageSerializer is injected and there is no guarantee that it serializes to json.
        bool isJson = IsValidJson(serializedMessage);
        blob["datacontenttype"] = dataContentType;

        if (IsJsonContentType(dataContentType))
        {
            if (isJson)
            {
                // If it's valid JSON, parse it to maintain structure
                blob["data"] = JsonNode.Parse(serializedMessage);
            }
            else
            {
                // If it's not valid JSON but content type indicates JSON,
                // log warning and store as string
                _logger.LogWarning("Data was serialized as non-JSON, but datacontenttype indicates JSON format. Storing as string.");
                blob["data"] = serializedMessage;
            }
        }
        else
        {
            // For non-JSON content types, store as string
            blob["data"] = serializedMessage;
        }
    }

    private string ExtractDataContent(JsonElement dataElement, string? dataContentType)
    {
        return IsJsonContentType(dataContentType)
            ? dataElement.ValueKind == JsonValueKind.String
                ? dataElement.GetString()!
                : dataElement.GetRawText()
            : dataElement.GetString()
              ?? throw new InvalidDataException("Data must be a string for non-JSON content type");
    }

    /// <inheritdoc/>
    public async ValueTask<ConvertToEnvelopeResult> ConvertToEnvelopeAsync(Message sqsMessage)
    {
        try
        {
            // Parse and validate the message envelope
            var parsedResult = await ParseMessageEnvelope(sqsMessage);

            // Get subscriber mapping and deserialize message
            var subscriberMapping = GetAndValidateSubscriberMapping(parsedResult.Envelope.MessageTypeIdentifier);
            var deserializedMessage = DeserializeDataContent(
                parsedResult.Envelope.Message!,
                parsedResult.Envelope.DataContentType,
                subscriberMapping.MessageType);

            // Create and populate final envelope
            var finalEnvelope = CreateFinalEnvelope(
                parsedResult.Envelope,
                parsedResult.Configuration,
                subscriberMapping.MessageType,
                deserializedMessage);

            await InvokePostDeserializationCallback(finalEnvelope);
            var result = new ConvertToEnvelopeResult(finalEnvelope, subscriberMapping);

            _logger.LogTrace("Created a generic {MessageEnvelopeName} of type '{MessageEnvelopeType}'",
                nameof(MessageEnvelope), result.Envelope.GetType());
            return result;
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

    private async Task<ParsedEnvelopeResult> ParseMessageEnvelope(Message sqsMessage)
    {
        sqsMessage.Body = await InvokePreDeserializationCallback(sqsMessage.Body);
        var config = GetMessageEnvelopeConfiguration(sqsMessage);

        using var document = JsonDocument.Parse(config.MessageEnvelopeBody!);
        var root = document.RootElement;

        if (!root.TryGetProperty("data", out var dataElement))
        {
            throw new InvalidDataException("Message envelope is missing required 'data' field");
        }

        string? dataContentType = root.TryGetProperty("datacontenttype", out var contentTypeElement)
            ? contentTypeElement.GetString()
            : "application/json";

        string dataContent = ExtractDataContent(dataElement, dataContentType);

        // Create intermediate envelope with all properties
        var envelopeJson = new JsonObject();
        foreach (var property in root.EnumerateObject())
        {
            envelopeJson[property.Name] = property.Name == "data"
                ? JsonValue.Create(dataContent)
                : JsonNode.Parse(property.Value.GetRawText());
        }

        var intermediateEnvelope = JsonSerializer.Deserialize<MessageEnvelope<string>>(envelopeJson.ToJsonString())!;
        ValidateMessageEnvelope(intermediateEnvelope);

        return new ParsedEnvelopeResult(intermediateEnvelope, config);
    }

    private MessageEnvelope CreateFinalEnvelope(
        MessageEnvelope<string> intermediateEnvelope,
        MessageEnvelopeConfiguration config,
        Type messageType,
        object deserializedMessage)
    {
        var messageEnvelopeType = typeof(MessageEnvelope<>).MakeGenericType(messageType);

        if (Activator.CreateInstance(messageEnvelopeType) is not MessageEnvelope finalEnvelope)
        {
            _logger.LogError(
                $"Failed to create a {nameof(MessageEnvelope)} of type '{{MessageEnvelopeType}}'",
                messageEnvelopeType.FullName);
            throw new InvalidOperationException(
                $"Failed to create a {nameof(MessageEnvelope)} of type '{messageEnvelopeType.FullName}'");
        }

        finalEnvelope.Id = intermediateEnvelope.Id;
        finalEnvelope.Source = intermediateEnvelope.Source;
        finalEnvelope.Version = intermediateEnvelope.Version;
        finalEnvelope.MessageTypeIdentifier = intermediateEnvelope.MessageTypeIdentifier;
        finalEnvelope.TimeStamp = intermediateEnvelope.TimeStamp;
        finalEnvelope.Metadata = intermediateEnvelope.Metadata;
        finalEnvelope.DataContentType = intermediateEnvelope.DataContentType;
        finalEnvelope.SQSMetadata = config.SQSMetadata;
        finalEnvelope.SNSMetadata = config.SNSMetadata;
        finalEnvelope.EventBridgeMetadata = config.EventBridgeMetadata;
        finalEnvelope.SetMessage(deserializedMessage);

        return finalEnvelope;
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

    private object DeserializeDataContent(string dataContent, string? dataContentType, Type messageType)
    {
        if (IsJsonContentType(dataContentType))
        {
            return _messageSerializer.Deserialize(dataContent, messageType);
        }

        if (messageType == typeof(string))
        {
            return dataContent;
        }

        throw new InvalidOperationException(
            $"Cannot deserialize non-JSON content type {dataContentType} to type {messageType}");
    }

    private bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            // If datacontenttype is unspecified, treat as application/json
            return true;
        }

        // Strip any parameters (anything after ';')
        var mediaType = contentType.Split(';')[0].Trim().ToLowerInvariant();

        return mediaType.EndsWith("/json") || // Matches */json
               (mediaType.Contains('/') && mediaType.EndsWith("+json")); // Matches */*+json
    }

    private bool IsValidJson(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput)) return false;

        try
        {
            JsonDocument.Parse(strInput);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void ValidateMessageEnvelope<T>(MessageEnvelope<T>? messageEnvelope)
    {
        if (messageEnvelope is null)
        {
            _logger.LogError("{MessageEnvelope} cannot be null", nameof(messageEnvelope));
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
            _logger.LogError("MessageEnvelope instance is not valid" + Environment.NewLine +"{ValidationFailures}", validationFailures);
            throw new InvalidDataException($"MessageEnvelope instance is not valid{Environment.NewLine}{validationFailures}");
        }
    }

    private MessageEnvelopeConfiguration GetMessageEnvelopeConfiguration(Message sqsMessage)
    {
        var envelopeConfiguration = new MessageEnvelopeConfiguration();
        envelopeConfiguration.MessageEnvelopeBody = sqsMessage.Body;

        using (var document = JsonDocument.Parse(sqsMessage.Body))
        {
            var root = document.RootElement;
            // Check if the SQS message body contains an outer envelope injected by SNS.
            if (root.TryGetProperty("Type", out var messageType) && string.Equals("Notification", messageType.GetString()))
            {
                // Retrieve the inner message envelope.
                envelopeConfiguration.MessageEnvelopeBody = GetJsonPropertyAsString(root, "Message");
                if (string.IsNullOrEmpty(envelopeConfiguration.MessageEnvelopeBody))
                {
                    _logger.LogError("Failed to create a message envelope configuration because the SNS message envelope does not contain a valid message property.");
                    throw new FailedToCreateMessageEnvelopeConfigurationException("The SNS message envelope does not contain a valid message property.");
                }
                SetSNSMetadata(envelopeConfiguration, root);
            }
            // Check if the SQS message body contains an outer envelope injected by EventBridge.
            else if (root.TryGetProperty("detail", out var _)
                && root.TryGetProperty("id", out var _)
                && root.TryGetProperty("version", out var _)
                && root.TryGetProperty("region", out var _))
            {
                // Retrieve the inner message envelope.
                envelopeConfiguration.MessageEnvelopeBody = GetJsonPropertyAsString(root, "detail");
                if (string.IsNullOrEmpty(envelopeConfiguration.MessageEnvelopeBody))
                {
                    _logger.LogError("Failed to create a message envelope configuration because the EventBridge message envelope does not contain a valid 'detail' property.");
                    throw new FailedToCreateMessageEnvelopeConfigurationException("The EventBridge message envelope does not contain a valid 'detail' property.");
                }
                SetEventBridgeMetadata(envelopeConfiguration, root);
            }
        }

        SetSQSMetadata(envelopeConfiguration, sqsMessage);
        return envelopeConfiguration;
    }

    private class ParsedEnvelopeResult
    {
        public MessageEnvelope<string> Envelope { get; }
        public MessageEnvelopeConfiguration Configuration { get; }

        public ParsedEnvelopeResult(
            MessageEnvelope<string> envelope,
            MessageEnvelopeConfiguration configuration)
        {
            Envelope = envelope;
            Configuration = configuration;
        }
    }

    private void SetSQSMetadata(MessageEnvelopeConfiguration envelopeConfiguration, Message sqsMessage)
    {
        envelopeConfiguration.SQSMetadata = new SQSMetadata
        {
            MessageID = sqsMessage.MessageId,
            MessageAttributes = sqsMessage.MessageAttributes,
            ReceiptHandle = sqsMessage.ReceiptHandle
        };
        if (sqsMessage.Attributes.TryGetValue("MessageGroupId", out var attribute))
        {
            envelopeConfiguration.SQSMetadata.MessageGroupId = attribute;
        }
        if (sqsMessage.Attributes.TryGetValue("MessageDeduplicationId", out var messageAttribute))
        {
            envelopeConfiguration.SQSMetadata.MessageDeduplicationId = messageAttribute;
        }
    }

    private void SetSNSMetadata(MessageEnvelopeConfiguration envelopeConfiguration, JsonElement root)
    {
        envelopeConfiguration.SNSMetadata = new SNSMetadata
        {
            MessageId = GetJsonPropertyAsString(root, "MessageId"),
            TopicArn = GetJsonPropertyAsString(root, "TopicArn"),
            Subject = GetJsonPropertyAsString(root, "Subject"),
            UnsubscribeURL = GetJsonPropertyAsString(root, "UnsubscribeURL"),
            Timestamp = GetJsonPropertyAsDateTimeOffset(root, "Timestamp")
        };
        if (root.TryGetProperty("MessageAttributes", out var messageAttributes))
        {
            envelopeConfiguration.SNSMetadata.MessageAttributes = messageAttributes.Deserialize<Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>>();
        }
    }

    private void SetEventBridgeMetadata(MessageEnvelopeConfiguration envelopeConfiguration, JsonElement root)
    {
        envelopeConfiguration.EventBridgeMetadata = new EventBridgeMetadata
        {
            EventId = GetJsonPropertyAsString(root, "id"),
            Source = GetJsonPropertyAsString(root, "source"),
            DetailType = GetJsonPropertyAsString(root, "detail-type"),
            Time = GetJsonPropertyAsDateTimeOffset(root, "time"),
            AWSAccount = GetJsonPropertyAsString(root, "account"),
            AWSRegion = GetJsonPropertyAsString(root, "region"),
            Resources = GetJsonPropertyAsList<string>(root, "resources")
        };
    }

    private string? GetJsonPropertyAsString(JsonElement node, string propertyName)
    {
        if (node.TryGetProperty(propertyName, out var propertyValue))
        {
            return propertyValue.ValueKind switch
            {
                JsonValueKind.Object => propertyValue.GetRawText(),
                JsonValueKind.String => propertyValue.GetString(),
                JsonValueKind.Number => propertyValue.ToString(),
                JsonValueKind.True => propertyValue.ToString(),
                JsonValueKind.False => propertyValue.ToString(),
                _ => throw new InvalidDataException($"{propertyValue.ValueKind} cannot be converted to a string value"),
            };
        }
        return null;
    }

    private DateTimeOffset GetJsonPropertyAsDateTimeOffset(JsonElement node, string propertyName)
    {
        if (node.TryGetProperty(propertyName, out var propertyValue))
        {
            return JsonSerializer.Deserialize<DateTimeOffset>(propertyValue);
        }
        return DateTimeOffset.MinValue;
    }

    private List<T>? GetJsonPropertyAsList<T>(JsonElement node, string propertyName)
    {
        if (node.TryGetProperty(propertyName, out var propertyValue))
        {
            return JsonSerializer.Deserialize<List<T>>(propertyValue);
        }
        return null;
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
