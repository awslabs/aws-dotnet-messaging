// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Configuration for polling messages from SQS
/// </summary>
public class SQSMessagePollerConfiguration : IMessagePollerConfiguration
{
    /// <summary>
    /// The SQS QueueUrl to poll messages from.
    /// </summary>
    public string QueueUrl { get; }

    /// <summary>
    /// Construct an instance of <see cref="AWS.Messaging.Configuration.SQSMessagePollerConfiguration" />
    /// </summary>
    /// <param name="queueUrl">The SQS QueueUrl to poll messages from.</param>
    public SQSMessagePollerConfiguration(string queueUrl)
    {
        QueueUrl = queueUrl;
    }
}
