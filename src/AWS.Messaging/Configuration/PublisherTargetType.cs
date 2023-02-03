// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// The type of publisher used.
/// </summary>
public enum PublisherTargetType
{
    /// <summary>
    /// Specifies a publisher that will publish to SQS
    /// </summary>
    SQS,
    /// <summary>
    /// Specifies a publisher that will publish to SNS
    /// </summary>
    SNS,
    /// <summary>
    /// Specifies a publisher that will publish to EventBridge
    /// </summary>
    EventBridge
}
