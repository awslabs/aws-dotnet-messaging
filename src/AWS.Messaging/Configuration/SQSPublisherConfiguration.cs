// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// SQS implementation of <see cref="IMessagePublisherConfiguration"/>
/// </summary>
public class SQSPublisherConfiguration : IMessagePublisherConfiguration
{
    /// <summary>
    /// Retrieves the SQS Queue URL which the publisher will use to route the message.
    /// </summary>
    public string? PublisherEndpoint { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="SQSPublisherConfiguration"/>.
    /// </summary>
    /// <param name="queueUrl">The SQS Queue URL</param>
    public SQSPublisherConfiguration(string? queueUrl)
    {
        PublisherEndpoint = queueUrl;
    }
}
