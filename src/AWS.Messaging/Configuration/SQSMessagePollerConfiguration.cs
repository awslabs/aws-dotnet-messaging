// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Internal configuration for polling messages from SQS
/// </summary>
internal class SQSMessagePollerConfiguration : IMessagePollerConfiguration
{
    /// <summary>
    /// Default value for <see cref="MaxNumberOfConcurrentMessages"/>
    /// </summary>
    /// <remarks>The default value is 10 messages.</remarks>
    public const int DefaultMaxNumberOfConcurrentMessages = 10;

    /// <summary>
    /// The SQS QueueUrl to poll messages from.
    /// </summary>
    public string SubscriberEndpoint { get; }

    /// <summary>
    /// The maximum number of messages from this queue to process concurrently.
    /// </summary>
    /// <remarks><inheritdoc cref="DefaultMaxNumberOfConcurrentMessages" path="//remarks"/></remarks>
    public int MaxNumberOfConcurrentMessages { get; init; } = DefaultMaxNumberOfConcurrentMessages;

    /// <summary>
    /// Construct an instance of <see cref="SQSMessagePollerConfiguration" />
    /// </summary>
    /// <param name="queueUrl">The SQS QueueUrl to poll messages from.</param>
    public SQSMessagePollerConfiguration(string queueUrl)
    {
        if (string.IsNullOrEmpty(queueUrl))
            throw new InvalidSubscriberEndpointException("The SQS Queue URL cannot be empty.");

        SubscriberEndpoint = queueUrl;
    }
}
