// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
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
    private readonly JsonSerializerContext? _jsonSerializerContext;

    public MessageSerializer(ILogger<MessageSerializer> logger, IMessageConfiguration messageConfiguration, IMessageJsonSerializerContextContainer jsonContextContainer)
    {
        _logger = logger;
        _messageConfiguration= messageConfiguration;
        _jsonSerializerContext = jsonContextContainer.GetJsonSerializerContext();
    }

    /// <inheritdoc/>
    /// <exception cref="FailedToDeserializeApplicationMessageException"></exception>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "Consumers relying on trimming would have been required to call the AddAWSMessageBus overload that takes in JsonSerializerContext that will be used here to avoid the call that requires unreferenced code.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050",
        Justification = "Consumers relying on trimming would have been required to call the AddAWSMessageBus overload that takes in JsonSerializerContext that will be used here to avoid the call that requires unreferenced code.")]
    public object Deserialize(string message, Type deserializedType)
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

            if (_jsonSerializerContext != null)
            {
                return JsonSerializer.Deserialize(message, deserializedType, _jsonSerializerContext) ?? throw new JsonException("The deserialized application message is null.");
            }
            else
            {
                return JsonSerializer.Deserialize(message, deserializedType, jsonSerializerOptions) ?? throw new JsonException("The deserialized application message is null.");
            }
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

    /// <inheritdoc/>
    /// <exception cref="FailedToSerializeApplicationMessageException"></exception>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "Consumers relying on trimming would have been required to call the AddAWSMessageBus overload that takes in JsonSerializerContext that will be used here to avoid the call that requires unreferenced code.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050",
        Justification = "Consumers relying on trimming would have been required to call the AddAWSMessageBus overload that takes in JsonSerializerContext that will be used here to avoid the call that requires unreferenced code.")]
    public MessageSerializerResults Serialize(object message)
    {
        try
        {
            var jsonSerializerOptions = _messageConfiguration.SerializationOptions.SystemTextJsonOptions;

            string jsonString;
            Type messageType = message.GetType();

            if (_jsonSerializerContext != null)
            {
                jsonString = JsonSerializer.Serialize(message, messageType, _jsonSerializerContext);
            }
            else
            {
                jsonString = JsonSerializer.Serialize(message, jsonSerializerOptions);
            }

            if (_messageConfiguration.LogMessageContent)
            {
                _logger.LogTrace("Serialized the message object as the following raw string:\n{JsonString}", jsonString);
            }
            else
            {
                _logger.LogTrace("Serialized the message object to a raw string with a content length of {ContentLength}.", jsonString.Length);
            }

            return new MessageSerializerResults(jsonString, "application/json");
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
