// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Publishers;

namespace AWS.Messaging.Services;

/// <summary>
/// This interface allows sending messages from application code to recipient-specific Amazon services.
/// It exposes the <see cref="SendAsync{T}(T, CancellationToken)"/> method which takes in a user-defined message to send to a recipient-specific Amazon service.
/// </summary>
public interface ICommandPublisher
{
    /// <summary>
    /// Sends the application message to a recipient-specific Amazon service.
    /// </summary>
    /// <param name="message">The application message that will be serialized and sent.</param>
    /// <param name="token">The cancellation token used to cancel the request.</param>
    Task<IPublishResponse> SendAsync<T>(T message, CancellationToken token = default);
}
