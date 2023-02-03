// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Implementation of <see cref="IPublisherMapping"/>.
/// </summary>
public class PublisherMapping : IPublisherMapping
{
    /// <inheritdoc/>
    public string PublishTargetType { get; init; }

    /// <inheritdoc/>
    public Type MessageType { get; init; }

    /// <inheritdoc/>
    public string MessageTypeIdentifier { get; init; }

    /// <inheritdoc/>
    public IMessagePublisherConfiguration? PublisherConfiguration { get; init; }

    /// <summary>
    /// Creates a mapping object for the specified message type as well as the AWS service to publisher to.
    /// This object will be used internally by the system to properly route the user-defined message.
    /// </summary>
    /// <param name="messageType">The Type of the message</param>
    /// <param name="publishTargetId">The AWS service endpoint, such as SQS Queue URL, SNS Topic URL ...</param>
    /// <param name="publishTargetType">The type of publisher to use</param>
    /// <param name="messageTypeIdentifier">The language-agnostic message type identifier. If not specified, the .NET type will be used.</param>
    public PublisherMapping(Type messageType, string publishTargetId, string publishTargetType, string? messageTypeIdentifier = null)
    {
        PublishTargetType = publishTargetType;
        MessageType = messageType;
        MessageTypeIdentifier =
            !string.IsNullOrEmpty(messageTypeIdentifier) ?
            messageTypeIdentifier :
            messageType.FullName ?? throw new InvalidMessageTypeException("Unable to retrive the Full Name of the provided Message Type.");
        switch (publishTargetType)
        {
            case nameof(PublisherTargetType.SQS):
                PublisherConfiguration = new SQSPublisherConfiguration(publishTargetId);
                break;
            case nameof(PublisherTargetType.SNS):
                PublisherConfiguration = new SNSPublisherConfiguration(publishTargetId);
                break;
            case nameof(PublisherTargetType.EventBridge):
                PublisherConfiguration = new EventBridgePublisherConfiguration(publishTargetId);
                break;
        }
    }
}
