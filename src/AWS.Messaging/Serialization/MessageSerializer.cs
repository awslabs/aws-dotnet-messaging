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

    /// <inheritdoc/>
    /// <exception cref="FailedToDeserializeApplicationMessage"></exception>
    public object Deserialize(string message, Type deserializedType)
    {
        try
        {
            var jsonSerializerOptions = _messageConfiguration.SerializationOptions.SystemTextJsonOptions;
            _logger.LogTrace("Deserializing the following message into type '{0}':\n{1}", deserializedType, message);
            return JsonSerializer.Deserialize(message, deserializedType, jsonSerializerOptions) ?? throw new JsonException("The deserialized application message is null.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize application message into an instance of {0}.", deserializedType);
            throw new FailedToDeserializeApplicationMessage($"Failed to deserialize application message into an instance of {deserializedType}.", ex);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="FailedToSerializeApplicationMessage"></exception>
    public string Serialize(object message)
    {
        try
        {
            var jsonSerializerOptions = _messageConfiguration.SerializationOptions.SystemTextJsonOptions;
            var jsonString = JsonSerializer.Serialize(message, jsonSerializerOptions);
            _logger.LogTrace("Serialized the message object as the following raw string:\n{0}", jsonString);
            return jsonString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize application message into a string");
            throw new FailedToSerializeApplicationMessage("Failed to serialize application message into a string", ex);
        }
    }
}
