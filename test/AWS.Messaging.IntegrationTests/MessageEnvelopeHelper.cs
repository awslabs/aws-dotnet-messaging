using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AWS.Messaging.IntegrationTests;

/// <summary>
/// Helper class to handle JSON deserialization for nested message envelopes.
/// </summary>
public static class MessageEnvelopeHelper
{
    /// <summary>
    /// Deserializes a message containing nested JSON structures, handling both EventBridge and SNS formats.
    /// </summary>
    /// <param name="jsonMessage">The raw JSON message.</param>
    /// <param name="messageSource">The source of the message (e.g., "EventBridge" or "SNS").</param>
    /// <returns>Tuple containing the message envelope and the deserialized inner message.</returns>
    public static (MessageEnvelope<object> Envelope, object DeserializedMessage) DeserializeNestedMessage(string jsonMessage, string messageSource)
    {
        using var document = JsonDocument.Parse(jsonMessage);
        var root = document.RootElement;

        // Handle different message sources
        JsonElement messageEnvelopeElement = messageSource.ToLowerInvariant() switch
        {
            "eventbridge" => root.GetProperty("detail"),
            "sns" => JsonDocument.Parse(root.GetProperty("Message").GetString()).RootElement,
            "sqs" => root,
            _ => throw new ArgumentException($"Unsupported message source: {messageSource}")
        };

        // Create a copy without the data field
        var envelopeJson = new JsonObject();
        foreach (var property in messageEnvelopeElement.EnumerateObject())
        {
            if (property.Name != "data")
            {
                envelopeJson.Add(property.Name, JsonNode.Parse(property.Value.GetRawText()));
            }
        }

        // Deserialize the envelope
        var envelope = JsonSerializer.Deserialize<MessageEnvelope<object>>(envelopeJson.ToJsonString());

        // Get the data separately
        var dataElement = messageEnvelopeElement.GetProperty("data");
        var messageType = Type.GetType(envelope.MessageTypeIdentifier);
        if (messageType == null)
        {
            throw new InvalidOperationException($"Could not find type: {envelope.MessageTypeIdentifier}");
        }

        var deserializedMessage = JsonSerializer.Deserialize(dataElement.GetRawText(), messageType);

        return (envelope, deserializedMessage);
    }
}
