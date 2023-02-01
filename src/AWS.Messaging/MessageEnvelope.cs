// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace AWS.Messaging;

/// <summary>
/// Abstract MessageEnvelope containing all of the envelope information without the specific message.
/// This class adheres to the CloudEvents specification v1.0 and contains all the attributes that are marked as required by the spec.
/// The CloudEvent spec can be found <see href="https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md">here.</see>
/// </summary>
public abstract class MessageEnvelope
{
    /// <summary>
    /// Specifies the envelope ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Specifies the source of the event.
    /// This can be the organization publishing the event or the process that produced the event.
    /// </summary>
    [JsonPropertyName("source")]
    public Uri Source { get; set; }

    /// <summary>
    /// The version of the CloudEvents specification which the event uses.
    /// </summary>
    [JsonPropertyName("specversion")]
    public string Version { get; set; }

    /// <summary>
    /// The type of event that occurred. This represents the language agnostic type that is used to deserialize the envelope message into a .NET type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The timestamp when the event occurred.
    /// </summary>
    [JsonPropertyName("time")]
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// This stores different metadata that is not modeled as a top-level property in MessageEnvelope class.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Stores metadata related to Amazon SQS.
    /// </summary>
    public SQSMetadata? SQSMetadata { get; set; }

    /// <summary>
    /// Creates a MessageEnvelope object that is aligned with CloudEvents specification v1.0
    /// </summary>
    public MessageEnvelope(string id,
        Uri source,
        string version,
        string type,
        DateTime timeStamp)
    {
        Id = id;
        Source = source;
        Version = version;
        Type = type;
        TimeStamp = timeStamp;
    }
}

/// <summary>
/// Generic class for MessageEnvelope objects.
/// This class adheres to the CloudEvents specification v1.0 and contains all the attributes that are marked as required by the spec.
/// The CloudEvent spec can be found <see href="https://github.com/cloudevents/spec/blob/main/cloudevents/spec.md">here.</see>
/// </summary>
public class MessageEnvelope<T> : MessageEnvelope
{
    /// <summary>
    /// Creates a MessageEnvelope object that is aligned with CloudEvents specification v1.0
    /// </summary>
    public MessageEnvelope(string id,
        Uri source,
        string version,
        string type,
        DateTime timeStamp,
        T message)
        : base(id, source, version, type, timeStamp)
    {
        Message = message;
    }

    /// <summary>
    /// The application message that will be processed.
    /// </summary>
    [JsonPropertyName("data")]
    public T Message { get; set; }
}
