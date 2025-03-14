// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Amazon.SQS.Model;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace AWS.Messaging.Serialization.Handlers;

/// <summary>
/// Handles the creation of metadata objects from various AWS messaging services.
/// </summary>
internal static class MessageMetadataHandler
{
    /// <summary>
    /// Creates SQS metadata from an SQS message.
    /// </summary>
    /// <param name="message">The SQS message containing metadata information.</param>
    /// <returns>An SQSMetadata object containing the extracted metadata.</returns>
    public static SQSMetadata CreateSQSMetadata(Message message)
    {
        var metadata = new SQSMetadata
        {
            MessageID = message.MessageId,
            ReceiptHandle = message.ReceiptHandle,
            MessageAttributes = message.MessageAttributes,
        };

        // Get FIFO queue attributes from message.Attributes
        if (message.Attributes != null)
        {
            if (message.Attributes.TryGetValue("MessageGroupId", out var groupId))
            {
                metadata.MessageGroupId = groupId;
            }
            if (message.Attributes.TryGetValue("MessageDeduplicationId", out var deduplicationId))
            {
                metadata.MessageDeduplicationId = deduplicationId;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Creates SNS metadata from a JSON element representing an SNS message.
    /// </summary>
    /// <param name="root">The root JSON element containing SNS metadata information.</param>
    /// <returns>An SNSMetadata object containing the extracted metadata.</returns>
    public static SNSMetadata CreateSNSMetadata(JsonElement root)
    {
        var metadata = new SNSMetadata
        {
            MessageId = root.GetProperty("MessageId").GetString(),
            TopicArn = root.GetProperty("TopicArn").GetString(),
            Timestamp = root.GetProperty("Timestamp").GetDateTimeOffset(),
            UnsubscribeURL = root.GetProperty("UnsubscribeURL").GetString(),
        };

        if (root.TryGetProperty("Subject", out var subject))
        {
            metadata.Subject = subject.GetString();
        }

        if (root.TryGetProperty("MessageAttributes", out var messageAttributes))
        {
            metadata.MessageAttributes = JsonSerializer.Deserialize<Dictionary<string, MessageAttributeValue>>(messageAttributes);
        }

        return metadata;
    }

    /// <summary>
    /// Creates EventBridge metadata from a JSON element representing an EventBridge event.
    /// </summary>
    /// <param name="root">The root JSON element containing EventBridge metadata information.</param>
    /// <returns>An EventBridgeMetadata object containing the extracted metadata.</returns>
    public static EventBridgeMetadata CreateEventBridgeMetadata(JsonElement root)
    {
        return new EventBridgeMetadata
        {
            EventId = root.GetProperty("id").GetString(),
            DetailType = root.GetProperty("detail-type").GetString(),
            Source = root.GetProperty("source").GetString(),
            AWSAccount = root.GetProperty("account").GetString(),
            Time = root.GetProperty("time").GetDateTimeOffset(),
            AWSRegion = root.GetProperty("region").GetString(),
            Resources = root.GetProperty("resources").EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => x != null)
                .ToList()!
        };
    }
}
