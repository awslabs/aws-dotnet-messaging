// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// Instances of <see cref="AWS.Messaging.Services.IMessagePoller" /> handle polling for messages from the underlying AWS service. Once
/// messages are received from the underlying service they are deserialized into <see cref="AWS.Messaging.MessageEnvelope{T}"/>;
/// and handed off to the IMessageManager for processing.
///
/// The <see cref="AWS.Messaging.Services.IMessagePoller" /> abstracts around the underlying service and also provides the utility methods used
/// for adjusting the message's lifecycle.
/// </summary>
public interface IMessagePoller
{
    /// <summary>
    /// How frequently message visibility should be extended in seconds
    /// via <see cref="ExtendMessageVisibilityTimeoutAsync"/> while the message is still being processed
    /// </summary>
    int VisibilityTimeoutExtensionInterval { get; }

    /// <summary>
    /// Start polling the underlying service. Polling will run indefinitely till the CancellationToken is cancelled.
    /// </summary>
    /// <param name="token">Optional cancellation token to shutdown the poller.</param>
    Task StartPollingAsync(CancellationToken token = default);

    /// <summary>
    /// Delete the message in the underlying service.
    /// </summary>
    /// <param name="messages">The messages to delete.</param>
    /// <param name="token">Optional token to cancel the deletion.</param>
    Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default);

    /// <summary>
    /// Inform the underlying service to extend the message's visibility timeout because the message is still being processed.
    /// </summary>
    /// <param name="messages">The messages to extend their visibility timeout.</param>
    /// <param name="token">Optional token to cancel the visibility timeout extension.</param>
    Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default);
}
