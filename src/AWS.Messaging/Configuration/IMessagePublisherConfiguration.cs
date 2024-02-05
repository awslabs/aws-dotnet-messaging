// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Interface for the different message publisher configuration.
/// </summary>
public interface IMessagePublisherConfiguration
{
    /// <summary>
    /// Retrieves the AWS service-specific endpoint URL which the publisher will use to route the message.
    /// </summary>
    string? PublisherEndpoint { get; set; }
}
