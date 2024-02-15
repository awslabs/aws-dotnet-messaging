// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Publishers.EventBridge;

namespace AWS.Messaging.Configuration;

/// <summary>
/// EventBridge implementation of <see cref="IMessagePublisherConfiguration"/>
/// </summary>
public class EventBridgePublisherConfiguration : IMessagePublisherConfiguration
{
    /// <summary>
    /// Retrieves the EventBridge Event Bus name or ARN which the publisher will use to route the message.
    /// </summary>
    /// <remarks>
    /// If the event bus name is null, a message-specific event bus must be set on the
    /// <see cref="EventBridgeOptions"/> when sending an event.
    /// </remarks>
    public string? PublisherEndpoint { get; set; }

    /// <summary>
    /// The ID of the global EventBridge endpoint.
    /// </summary>
     /// <remarks>
    /// If the endpoint ID is null, a message-specific event bus may be set on the
    /// <see cref="EventBridgeOptions"/> when sending an event.
    /// </remarks>
    public string? EndpointID { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="EventBridgePublisherConfiguration"/>.
    /// </summary>
    /// <param name="eventBusName">The name or the ARN of the event bus where the message is published.
    /// If the event bus name is null, a message-specific event bus must be set on the
    /// <see cref="EventBridgeOptions"/> when sending an event.</param>
    public EventBridgePublisherConfiguration(string? eventBusName)
    {
        PublisherEndpoint = eventBusName;
    }
}
