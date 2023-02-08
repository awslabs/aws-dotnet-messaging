// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Interface for the message publisher builder configuration.
/// </summary>
public interface IMessageConfiguration
{
    /// <summary>
    /// Maps the message types to a publisher configuration
    /// </summary>
    IList<PublisherMapping> PublisherMappings { get; }

    /// <summary>
    /// Returns back the publisher mapping for the given message type.
    /// </summary>
    /// <param name="messageType">The Type of the message</param>
    /// <returns>The <see cref="PublisherMapping"/> containing routing info for the specified message type.</returns>
    PublisherMapping? GetPublisherMapping(Type messageType);
}
