using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AWS.Messaging.Serialization
{
    internal class JsonMessageSerializer : IMessageSerializer
    {
        private readonly ILogger<JsonMessageSerializer> _logger;
        private readonly IMessageConfiguration _messageConfiguration;

        public JsonMessageSerializer(ILogger<JsonMessageSerializer> logger, IMessageConfiguration messageConfiguration)
        {
            _logger = logger;
            _messageConfiguration = messageConfiguration;
        }

        public string DataContentType => "application/json";

        public string Serialize(object message)
        {
            try
            {
                var jsonSerializerOptions = _messageConfiguration.SerializationOptions.SystemTextJsonOptions;
                var json = JsonSerializer.Serialize(message, jsonSerializerOptions);
                
                if (_messageConfiguration.LogMessageContent)
                {
                    _logger.LogTrace("Serialized the message object as the following JSON:\n{JsonString}", json);
                }
                else
                {
                    _logger.LogTrace("Serialized the message object to JSON");
                }

                return json;
            }
            catch (JsonException) when (!_messageConfiguration.LogMessageContent)
            {
                _logger.LogError("Failed to serialize application message to JSON");
                throw new FailedToSerializeApplicationMessageException("Failed to serialize application message to JSON");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize application message to JSON");
                throw new FailedToSerializeApplicationMessageException("Failed to serialize application message to JSON", ex);
            }
        }

        public object Deserialize(string data, Type deserializedType)
        {
            try
            {
                var jsonSerializerOptions = _messageConfiguration.SerializationOptions.SystemTextJsonOptions;
                
                if (_messageConfiguration.LogMessageContent)
                {
                    _logger.LogTrace("Deserializing the following message into type '{DeserializedType}':\n{Message}", 
                        deserializedType, data);
                }
                else
                {
                    _logger.LogTrace("Deserializing message into type '{DeserializedType}'", deserializedType);
                }

                return JsonSerializer.Deserialize(data, deserializedType, jsonSerializerOptions) ?? 
                    throw new JsonException("The deserialized application message is null.");
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

        public IDictionary<string, object> ParseEnvelope(string envelopeData)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<Dictionary<string, object>>(envelopeData);
                return envelope ?? throw new InvalidOperationException("Failed to parse envelope data");
            }
            catch (Exception ex)
            {
                throw new FailedToDeserializeMessageEnvelopeException("Failed to parse envelope data", ex);
            }
        }

        public string CreateEnvelope(IDictionary<string, object> envelopeData)
        {
            try
            {
                return JsonSerializer.Serialize(envelopeData);
            }
            catch (Exception ex)
            {
                throw new FailedToSerializeMessageEnvelopeException("Failed to create envelope", ex);
            }
        }
    }
} 