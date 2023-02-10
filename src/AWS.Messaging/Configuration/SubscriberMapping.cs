// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Maps the <see cref="IMessageHandler{T}"/> to the type of message being processed.
/// </summary>
public class SubscriberMapping : ISubscriberMapping
{
    /// <inheritdoc/>
    public Type HandlerType { get; }

    /// <inheritdoc/>
    public Type MessageType { get; }

    /// <inheritdoc/>
    public string MessageTypeIdentifier { get; }

    /// <summary>
    /// Constructs an instance of <see cref="SubscriberMapping"/>
    /// </summary>
    /// <param name="handlerType">The type that implements <see cref="IMessageHandler{T}"/></param>
    /// <param name="messageType">The type that will be message data will deserialized into</param>
    /// <param name="messageTypeIdentifier">Optional message type identifier. If not set the full name of the <see cref="MessageType"/> is used.</param>
    public SubscriberMapping(Type handlerType, Type messageType, string? messageTypeIdentifier = null)
    {
        HandlerType = handlerType;
        MessageType = messageType;
        MessageTypeIdentifier =
            !string.IsNullOrEmpty(messageTypeIdentifier) ?
            messageTypeIdentifier :
            messageType.FullName ?? throw new InvalidMessageTypeException("Unable to retrieve the Full Name of the provided Message Type.");
    }
}
