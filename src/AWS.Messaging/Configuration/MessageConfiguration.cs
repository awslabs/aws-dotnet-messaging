// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Serialization;

namespace AWS.Messaging.Configuration;

/// <summary>
/// Implementation of <see cref="IMessageConfiguration"/>.
/// </summary>
public class MessageConfiguration : IMessageConfiguration
{
    /// <inheritdoc/>
    public IList<PublisherMapping> PublisherMappings { get; } = new List<PublisherMapping>();

    /// <inheritdoc/>
    public PublisherMapping? GetPublisherMapping(Type messageType)
    {
        var publisherMapping = PublisherMappings.FirstOrDefault(x => messageType == x.MessageType);
        return publisherMapping;
    }

    /// <inheritdoc/>
    public IList<SubscriberMapping> SubscriberMappings { get; } = new List<SubscriberMapping>();

    /// <inheritdoc/>
    public SubscriberMapping? GetSubscriberMapping(Type messageType)
    {
        var subscriberMapping = SubscriberMappings.FirstOrDefault(x => messageType == x.MessageType);
        return subscriberMapping;
    }

    /// <inheritdoc/>
    public SubscriberMapping? GetSubscriberMapping(string messageTypeIdentifier)
    {
        var subscriberMapping = SubscriberMappings.FirstOrDefault(x => messageTypeIdentifier == x.MessageTypeIdentifier);
        return subscriberMapping;
    }

    /// <inheritdoc/>
    public IList<IMessagePollerConfiguration> MessagePollerConfigurations { get; set; } = new List<IMessagePollerConfiguration>();

    /// <inheritdoc/>
    public SerializationOptions SerializationOptions { get; } = new SerializationOptions();

    /// <inheritdoc/>
    public IList<ISerializationCallback> SerializationCallbacks { get; } = new List<ISerializationCallback>();

    /// <inheritdoc/>
    public string? Source { get; set; }

    /// <inheritdoc/>
    public string? SourceSuffix { get; set; }

    /// <inheritdoc/>
    public bool LogMessageContent { get; set; }
}
