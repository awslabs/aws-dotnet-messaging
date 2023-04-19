// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging.Services;

/// <summary>
/// Identifies and invokes the correct method on a registered <see cref="IMessageHandler{T}"/> for received messages
/// </summary>
public interface IHandlerInvoker
{
    /// <summary>
    /// Identifies and calls the correct method on a registered <see cref="IMessageHandler{T}"/> for the given message
    /// </summary>
    /// <param name="messageEnvelope">Envelope of the message that is being handled</param>
    /// <param name="subscriberMapping">Subscriber mapping of the message that is being handled</param>
    /// <param name="token">Cancellation token which will be passed to the message handler</param>
    /// <returns>Task representing the outcome of the message handler</returns>
    Task<MessageProcessStatus> InvokeAsync(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping, CancellationToken token = default);
}
