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
    public Dictionary<string, MessageAttributeValue>? MessageAttributes { get; set; }

    /// <summary>
    /// A Universally Unique Identifier, unique for each message published.
    /// For a notification that Amazon SNS resends during a retry, the message ID of the original message is used.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// The ARN of the SNS topic where the message was published
    /// </summary>
    public string? TopicArn { get; set; }

    /// <summary>
    /// The Subject parameter specified when the notification was published to the topic.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// The timestamp when the notification was published
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// A URL that you can use to unsubscribe the endpoint from this topic.
    /// If you visit this URL, Amazon SNS unsubscribes the endpoint and stops sending notifications to this endpoint.
    /// </summary>
    public string? UnsubscribeURL { get; set; }

}
