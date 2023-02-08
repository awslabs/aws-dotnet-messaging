// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// SNS implementation of <see cref="IMessagePublisherConfiguration"/>
/// </summary>
public class SNSPublisherConfiguration : IMessagePublisherConfiguration
{
    /// <summary>
    /// Retrieves the SNS Topic URL which the publisher will use to route the message.
    /// </summary>
    public string PublisherEndpoint { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="SNSPublisherConfiguration"/>.
    /// </summary>
    /// <param name="topicUrl">The SNS Topic URL</param>
    public SNSPublisherConfiguration(string topicUrl)
    {
        if (string.IsNullOrEmpty(topicUrl))
            throw new InvalidPublisherEndpointException("The SNS Topic URL cannot be empty.");

        PublisherEndpoint = topicUrl;
    }
}
