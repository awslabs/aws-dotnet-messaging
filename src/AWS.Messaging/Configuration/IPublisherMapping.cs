// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Interface for a publisher mapping. This maps a message type to a message publisher.
/// </summary>
public interface IPublisherMapping
{
    /// <summary>
    /// The type of publisher that will publish the message.
    /// </summary>
    string PublishTargetType { get; init; }

    /// <summary>
    /// The .NET type used as the container for the message data.
    /// </summary>
    Type MessageType { get; init; }

    /// <summary>
    /// The identifier used as the indicator in the published message that maps to the PublisherType. If this
    /// is not set then the FullName of type specified in the MessageType is used.
    /// </summary>
    string MessageTypeIdentifier { get; init; }

    /// <summary>
    /// Configuration for publishing to an AWS service endpoint.
    /// </summary>
    IMessagePublisherConfiguration? PublisherConfiguration { get; init; }
}
