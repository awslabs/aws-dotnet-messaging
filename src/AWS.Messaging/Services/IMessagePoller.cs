// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// Instances of <see cref="AWS.Messaging.Services.IMessagePoller" /> handle polling for messages from the underlying AWS service. Once
/// messages are received from the underlying service they are deserialized into MessageEnvelop&lt;T&gt;
/// and handed off to the IMessageManager for processing.
///
/// The <see cref="AWS.Messaging.Services.IMessagePoller" /> abstracts around the underlying service and also provides the utility methods used
/// for adjusting the message's lifecycle.
/// </summary>
public interface IMessagePoller
{
    /// <summary>
    /// Start polling the underlying service. Polling will run indefinitely till the CancellationToken is cancelled.
    /// </summary>
    /// <param name="token">Optional cancellation token to shutdown the poller.</param>
    /// <returns></returns>
    Task StartPollingAsync(CancellationToken token = default(CancellationToken));

    /// <summary>
    /// Delete the message in the underlying service.
    /// </summary>
    /// <param name="messages">The messages to delete.</param>
    /// <returns></returns>
    Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages);

    /// <summary>
    /// Inform the underlying service to extend the message's visibility timeout because the message is still being processed.
    /// </summary>
    /// <param name="messages">The messages to extend their visibility timeout.</param>
    /// <returns></returns>
    Task ExtendMessageVisiblityTimeoutAsync(IEnumerable<MessageEnvelope> messages);
}
