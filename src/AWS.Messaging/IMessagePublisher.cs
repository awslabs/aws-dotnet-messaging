// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging;

/// <summary>
/// This interface will be enable users to publish messages from their application code to the configured AWS services.
/// It exposes the <see cref="PublishAsync{T}(T, CancellationToken)"/> method which will take in a user-defined message
/// and look up the corresponding <see cref="PublisherMapping"/> in order to route it to the appropriate AWS services.
/// Using dependency injection, this interface will be available to users to inject into their code.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// This method accepts a user-defined message which will then be published to an AWS service based on the
    /// configuration done during startup. It will look-up the <see cref="PublisherMapping"/> corresponding to the
    /// user message type, which contains the routing information of the provided message type.
    /// The method will wrap the message in a <see cref="MessageEnvelope"/> which contains metadata for the provided
    /// message which will enable the proper transporation of the message through the system.
    /// This method is accessible by injecting <see cref="IMessagePublisher"/> into the user's code
    /// using the dependency injection framework.
    /// </summary>
    Task PublishAsync<T>(T message, CancellationToken token = default);
}
