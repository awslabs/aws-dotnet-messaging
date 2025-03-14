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

        if (message.Attributes != null)
        {
            metadata.MessageGroupId = GetAttributeValue(message.Attributes, "MessageGroupId");
            metadata.MessageDeduplicationId = GetAttributeValue(message.Attributes, "MessageDeduplicationId");
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
            MessageId = GetStringProperty(root, "MessageId"),
            TopicArn = GetStringProperty(root, "TopicArn"),
            Timestamp = GetDateTimeOffsetProperty(root, "Timestamp") ?? default,
            UnsubscribeURL = GetStringProperty(root, "UnsubscribeURL"),
            Subject = GetStringProperty(root, "Subject")
        };

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
        var metadata = new EventBridgeMetadata
        {
            EventId = GetStringProperty(root, "id"),
            DetailType = GetStringProperty(root, "detail-type"),
            Source = GetStringProperty(root, "source"),
            AWSAccount = GetStringProperty(root, "account"),
            Time = GetDateTimeOffsetProperty(root, "time") ?? default,
            AWSRegion = GetStringProperty(root, "region"),
        };

        if (root.TryGetProperty("resources", out var resources))
        {
            metadata.Resources = resources.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => x != null)
                .ToList()!;
        }

        return metadata;
    }

    private static T? GetPropertyValue<T>(JsonElement root, string propertyName, Func<JsonElement, T> getValue)
    {
        return root.TryGetProperty(propertyName, out var property) ? getValue(property) : default;
    }

    private static string? GetStringProperty(JsonElement root, string propertyName)
        => GetPropertyValue(root, propertyName, element => element.GetString());

    private static DateTimeOffset? GetDateTimeOffsetProperty(JsonElement root, string propertyName)
        => GetPropertyValue(root, propertyName, element => element.GetDateTimeOffset());

    private static string? GetAttributeValue(Dictionary<string, string> attributes, string key)
    {
        return attributes.TryGetValue(key, out var value) ? value : null;
    }
}
