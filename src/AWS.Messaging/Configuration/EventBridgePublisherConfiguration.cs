// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// EventBridge implementation of <see cref="IMessagePublisherConfiguration"/>
/// </summary>
public class EventBridgePublisherConfiguration : IMessagePublisherConfiguration
{
    /// <summary>
    /// Retrieves the EventBridge Event Bus name or ARN which the publisher will use to route the message.
    /// </summary>
    public string PublisherEndpoint { get; set; }

    /// <summary>
    /// The ID of the global EventBridge endpoint.
    /// </summary>
    public string? EndpointID { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="EventBridgePublisherConfiguration"/>.
    /// </summary>
    /// <param name="eventBusName">The name or the ARN of the event bus where the message is published</param>
    public EventBridgePublisherConfiguration(string eventBusName)
    {
        if (string.IsNullOrEmpty(eventBusName))
            throw new InvalidPublisherEndpointException("The event bus name cannot be empty.");

        PublisherEndpoint = eventBusName;
    }
}
