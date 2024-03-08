// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// This interface allows publishing messages from application code to event-based Amazon services.
/// It exposes the <see cref="PublishAsync{T}(T, CancellationToken)"/> method which takes in a user-defined message to publish to an event-based Amazon service.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes the application message to an event-based Amazon service.
    /// </summary>
    /// <param name="message">The application message that will be serialized and published.</param>
    /// <param name="token">The cancellation token used to cancel the request.</param>
    Task PublishAsync<T>(T message, CancellationToken token = default);
}
