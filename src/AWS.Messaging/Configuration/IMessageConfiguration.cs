// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Interface for the message configuration.
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

    /// <summary>
    /// Maps the message types to a subscriber configuration
    /// </summary>
    IList<SubscriberMapping> SubscriberMappings { get; }

    /// <summary>
    /// Returns back the subscriber mapping for the given message type.
    /// </summary>
    /// <param name="messageType">The Type of the message</param>
    /// <returns>The <see cref="SubscriberMapping"/> containing routing info for the specified message type.</returns>
    SubscriberMapping? GetSubscriberMapping(Type messageType);

    /// <summary>
    /// List of configurations for subscriber to poll for messages from an AWS service endpoint.
    /// </summary>
    IList<IMessagePollerConfiguration> MessagePollerConfigurations { get; set; }

    /// <summary>
    /// Holds an instance of <see cref="SerializationOptions"/> to control the serialization/de-serialization of the application message.
    /// </summary>
    SerializationOptions SerializationOptions { get; }
}
