// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Nodes;

namespace AWS.Messaging.Serialization;

/// <summary>
///
/// </summary>
public class CloudEventEnvelopeSerializer
{
    private readonly IMessageSerializer _messageSerializer;

    /// <summary>
    ///
    /// </summary>
    /// <param name="messageSerializer"></param>
    public CloudEventEnvelopeSerializer(IMessageSerializer messageSerializer)
    {
        _messageSerializer = messageSerializer;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="jsonNode"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public MessageEnvelope<T> Deserialize<T>(JsonNode jsonNode)
    {

        var sourceString = jsonNode["source"]?.GetValue<string>() ?? string.Empty;
        Uri sourceUri;

        if (Uri.IsWellFormedUriString(sourceString, UriKind.Absolute))
        {
            sourceUri = new Uri(sourceString);
        }
        else
        {
            sourceUri = new Uri(sourceString, UriKind.Relative);
        }

        var envelope = new MessageEnvelope<T>
        {
            Id = jsonNode["id"]?.GetValue<string>() ?? string.Empty,
            Source = sourceUri,
            Version = jsonNode["specversion"]?.GetValue<string>() ?? string.Empty,
            MessageTypeIdentifier = jsonNode["type"]?.GetValue<string>() ?? string.Empty,
            TimeStamp = jsonNode["time"]?.GetValue<DateTimeOffset>() ?? DateTimeOffset.MinValue
        };

        envelope.SetMessage(_messageSerializer.Deserialize(jsonNode["data"]!, typeof(T)));

        // Handle metadata for any additional properties
        foreach (var property in jsonNode.AsObject())
        {
            if (!IsStandardProperty(property.Key) && property.Value != null)
            {
                envelope.Metadata[property.Key] = JsonSerializer.Deserialize<JsonElement>(property.Value.ToJsonString());
            }
        }

        return envelope;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="envelope"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public JsonNode Serialize<T>(MessageEnvelope<T> envelope)
    {
        var json = new JsonObject
        {
            ["id"] = envelope.Id,
            ["source"] = envelope.Source.ToString(),
            ["specversion"] = envelope.Version,
            ["type"] = envelope.MessageTypeIdentifier,
            ["time"] = envelope.TimeStamp.ToString("o")
        };

        // Serialize the message using MessageSerializer
        json["data"] = _messageSerializer.Serialize(envelope.Message!);

        // Add metadata
        foreach (var meta in envelope.Metadata)
        {
            json[meta.Key] = JsonNode.Parse(JsonSerializer.Serialize(meta.Value));
        }

        return json;
    }

    private bool IsStandardProperty(string propertyName)
    {
        return propertyName switch
        {
            "id" or "source" or "specversion" or "type" or "time" or "data" => true,
            _ => false
        };
    }
}

