// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Configuration for polling messages from SQS
/// </summary>
public class SQSMessagePollerOptions
{
    /// <inheritdoc cref="SQSMessagePollerConfiguration.MaxNumberOfConcurrentMessages"/>
    public int MaxNumberOfConcurrentMessages { get; set; } = SQSMessagePollerConfiguration.DefaultMaxNumberOfConcurrentMessages;
}

