// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Serialization;

/// <summary>
/// This is the default implementation of <see cref="IMessageSerializer"/> used by the framework.
/// It uses System.Text.Json to serialize and deserialize messages.
/// </summary>
internal class MessageSerializer : IMessageSerializer
{
    private readonly ILogger<MessageSerializer> _logger;
    private readonly IMessageConfiguration _messageConfiguration;

    public MessageSerializer(ILogger<MessageSerializer> logger, IMessageConfiguration messageConfiguration)
    {
        _logger = logger;
        _messageConfiguration= messageConfiguration;
    }

    /// <summary>
    /// Deserializes a JsonElement message into the specified type.
    /// </summary>
    /// <param name="message">The JsonElement containing the message to deserialize.</param>
    /// <param name="deserializedType">The target Type to deserialize the message into.</param>
    /// <returns>An object of the specified deserializedType containing the deserialized message data.</returns>
    /// <exception cref="FailedToDeserializeApplicationMessageException">Thrown when deserialization fails.</exception>
    /// <remarks>
    /// Uses System.Text.Json for deserialization with configuration options from IMessageConfiguration.
    /// Logging behavior is controlled by the LogMessageContent configuration setting.
    /// </remarks>
    public object Deserialize(JsonElement message, Type deserializedType)
    {
        try
        {
            var jsonSerializerOptions = _messageConfiguration.SerializationOptions.SystemTextJsonOptions;
            if (_messageConfiguration.LogMessageContent)
            {
                _logger.LogTrace("Deserializing the following message into type '{DeserializedType}':\n{Message}", deserializedType, message);
            }
            else
            {
                _logger.LogTrace("Deserializing the following message into type '{DeserializedType}'", deserializedType);
            }

            return JsonSerializer.Deserialize(message, deserializedType, jsonSerializerOptions) ?? throw new JsonException("The deserialized application message is null.");
        }
        catch (JsonException) when (!_messageConfiguration.LogMessageContent)
        {
            _logger.LogError("Failed to deserialize application message into an instance of {DeserializedType}.", deserializedType);
            throw new FailedToDeserializeApplicationMessageException($"Failed to deserialize application message into an instance of {deserializedType}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize application message into an instance of {DeserializedType}.", deserializedType);
            throw new FailedToDeserializeApplicationMessageException($"Failed to deserialize application message into an instance of {deserializedType}.", ex);
        }
    }

    /// <summary>
    /// Serializes an object into a JsonNode, maintaining the data in its JSON object form
    /// to align with CloudEvents specification for data content.
    /// </summary>
    /// <param name="message">The object to serialize.</param>
    /// <returns>A JsonNode representing the serialized message, preserving the JSON structure
    /// for direct use in CloudEvents data field.</returns>
    /// <exception cref="FailedToSerializeApplicationMessageException">Thrown when serialization fails.</exception>
    /// <remarks>
    /// Uses System.Text.Json for serialization with configuration options from IMessageConfiguration.
    /// Returns a JsonNode instead of a string to maintain the JSON structure, which is optimal for
    /// CloudEvents integration where the data field expects structured JSON content.
    /// Logging behavior is controlled by the LogMessageContent configuration setting.
    /// </remarks>
    public dynamic Serialize(object message)
    {
        try
        {
            var jsonSerializerOptions = _messageConfiguration.SerializationOptions.SystemTextJsonOptions;
            var jsonNode = JsonSerializer.SerializeToNode(message, jsonSerializerOptions);
            if (_messageConfiguration.LogMessageContent)
            {
                _logger.LogTrace("Serialized the message object as the following :\n{JsonString}", jsonNode);
            }

            return jsonNode!;
        }
        catch (JsonException) when (!_messageConfiguration.LogMessageContent)
        {
            _logger.LogError("Failed to serialize application message into a string");
            throw new FailedToSerializeApplicationMessageException("Failed to serialize application message into a string");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize application message into a string");
            throw new FailedToSerializeApplicationMessageException("Failed to serialize application message into a string", ex);
        }
    }
}
