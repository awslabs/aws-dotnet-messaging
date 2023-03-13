// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SimpleNotificationService.Model;

namespace AWS.Messaging;

/// <summary>
/// Contains Metadata related to Amazon SNS
/// </summary>
public class SNSMetadata
{
    /// <summary>
    /// Each message attribute consists of a Name, Type, and Value.For more information, <see href="https://docs.aws.amazon.com/sns/latest/dg/sns-message-attributes.html">refer</see> to Amazon SNS message attributes.
    /// </summary>
    public Dictionary<string, MessageAttributeValue> MessageAttributes { get; set; } = new Dictionary<string, MessageAttributeValue>();
}
