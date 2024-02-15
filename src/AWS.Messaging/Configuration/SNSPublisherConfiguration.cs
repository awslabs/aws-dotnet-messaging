// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Publishers.SNS;

namespace AWS.Messaging.Configuration;

/// <summary>
/// SNS implementation of <see cref="IMessagePublisherConfiguration"/>
/// </summary>
public class SNSPublisherConfiguration : IMessagePublisherConfiguration
{
    /// <summary>
    /// Retrieves the SNS Topic URL which the publisher will use to route the message.
    /// </summary>
    /// <remarks>
    /// If the topic URL is null, a message-specific topic URL must be set on the
    /// <see cref="SNSOptions"/> when publishing a message.
    /// </remarks>
    public string? PublisherEndpoint { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="SNSPublisherConfiguration"/>.
    /// </summary>
    /// <param name="topicUrl">The SNS Topic URL. If the topic URL is null, a message-specific topic URL must be set on the
    /// <see cref="SNSOptions"/> when publishing a message.</param>
    public SNSPublisherConfiguration(string? topicUrl)
    {
        PublisherEndpoint = topicUrl;
    }
}
