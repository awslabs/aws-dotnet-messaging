// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.Publishers;
using AWS.Messaging.Publishers.SQS;

namespace AWS.Messaging;

/// <summary>
/// This interface allows publishing messages from application code to configured AWS services.
/// It exposes the <see cref="PublishAsync{T}(T, CancellationToken)"/> method which takes in a user-defined message
/// and looks up the corresponding <see cref="PublisherMapping"/> in order to route it to the appropriate AWS services.
/// Using dependency injection, this interface is available to inject anywhere in the code.
/// </summary>
/// <remarks>
/// This is the generic publisher, which can publish multiple message types to any of the
/// supported AWS services. To set service-specific options when publishing, use the service-specific
/// publisher interface (such as <see cref="ISQSPublisher"/> for SQS) instead.
/// </remarks>
public interface IMessagePublisher
{
    /// <summary>
    /// This method accepts a user-defined message which is then published to an AWS service based on the
    /// configuration done during startup. It retrieves the <see cref="PublisherMapping"/> corresponding to the
    /// message type, which contains the routing information of the provided message type.
    /// The method wraps the message in a <see cref="MessageEnvelope"/> which contains metadata for the provided
    /// message that enables the proper transportation of the message throughout the framework.
    /// This method is accessible by injecting <see cref="IMessagePublisher"/> into the application code
    /// using the dependency injection framework.
    /// </summary>
    /// <exception cref="FailedToPublishException">If the message failed to publish. The inner exception contains more details if failures arise from the SDK.</exception>
    Task<IPublishResponse> PublishAsync<T>(T message, CancellationToken token = default);
}
