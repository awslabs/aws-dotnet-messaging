// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Nodes;
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
    public dynamic Serialize(object message)
    {
        return JsonSerializer.SerializeToNode(message)!;
    }


    /// <inheritdoc/>
    public object Deserialize(dynamic message, Type deserializedType)
    {
        try
        {
            if (message is JsonNode jsonNode)
            {
                var jsonString = jsonNode.ToJsonString();
                if (_messageConfiguration.LogMessageContent)
                {
                    _logger.LogTrace("Deserializing the following message into type '{DeserializedType}':\n{Message}", deserializedType, jsonString);
                }
                else
                {
                    _logger.LogTrace("Deserializing the following message into type '{DeserializedType}'", deserializedType);
                }

                return JsonSerializer.Deserialize(jsonString, deserializedType, _messageConfiguration.SerializationOptions.SystemTextJsonOptions)
                       ?? throw new JsonException("The deserialized application message is null.");
            }

            throw new ArgumentException("Message must be a JsonNode", nameof(message));
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


    public string GetDataContentType() => "application/json";
}
