// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Interface for a subscriber mapping. This maps a message type to a message handler.
/// </summary>
public interface ISubscriberMapping
{
    /// <summary>
    /// The <see cref="IMessageHandler{T}"/> that will process the message.
    /// </summary>
    Type HandlerType { get; }

    /// <summary>
    /// The .NET type used as the container for the message data.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// The identifier used as the indicator in the incoming message that maps to the <see cref="HandlerType"/>. If this
    /// is not set then the FullName of type specified in the <see cref="MessageType"/> is used.
    /// </summary>
    string MessageTypeIdentifier { get; }
}
