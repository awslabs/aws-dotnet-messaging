// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// The type of publisher used.
/// </summary>
public static class PublisherTargetType
{
    /// <summary>
    /// Specifies a publisher that will publish to SQS
    /// </summary>
    public const string SQS_PUBLISHER = "SQS";
    /// <summary>
    /// Specifies a publisher that will publish to SNS
    /// </summary>
    public const string SNS_PUBLISHER = "SNS";
    /// <summary>
    /// Specifies a publisher that will publish to EventBridge
    /// </summary>
    public const string EVENTBRIDGE_PUBLISHER = "EventBridge";
}
