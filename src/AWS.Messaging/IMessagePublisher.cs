// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging;

/// <summary>
/// This interface allows publishing messages from application code to configured AWS services.
/// It exposes the <see cref="PublishAsync{T}(T, CancellationToken)"/> method which takes in a user-defined message
/// and looks up the corresponding <see cref="PublisherMapping"/> in order to route it to the appropriate AWS services.
/// Using dependency injection, this interface is available to inject anywhere in the code.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// This method accepts a user-defined message which is then published to an AWS service based on the
    /// configuration done during startup. It retrieves the <see cref="PublisherMapping"/> corresponding to the
    /// message type, which contains the routing information of the provided message type.
    /// The method wraps the message in a <see cref="MessageEnvelope"/> which contains metadata for the provided
    /// message that enables the proper transportation of the message throughout the system.
    /// This method is accessible by injecting <see cref="IMessagePublisher"/> into the application code
    /// using the dependency injection framework.
    /// </summary>
    Task PublishAsync<T>(T message, CancellationToken token = default);
}
