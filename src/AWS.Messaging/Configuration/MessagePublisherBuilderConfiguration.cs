// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Implementation of <see cref="IMessagePublisherBuilderConfiguration"/>.
/// </summary>
public class MessagePublisherBuilderConfiguration : IMessagePublisherBuilderConfiguration
{
    /// <inheritdoc/>
    public IList<PublisherMapping> PublisherMappings { get; } = new List<PublisherMapping>();

    /// <inheritdoc/>
    public PublisherMapping? GetPublisherMapping(Type messageType)
    {
        var publisherMapping = PublisherMappings.FirstOrDefault(x => messageType == x.MessageType);
        return publisherMapping;
    }
}
