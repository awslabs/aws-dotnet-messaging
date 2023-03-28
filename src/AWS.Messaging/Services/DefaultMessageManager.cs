// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging.Services;

/// <inheritdoc/>
public class DefaultMessageManager : IMessageManager
{
    private readonly IMessagePoller _messagePoller;
    private readonly HandlerInvoker _handlerInvoker;
    /// <inheritdoc/>
    public DefaultMessageManager(IMessagePoller messagePoller, HandlerInvoker handlerInvoker)
    {
        _messagePoller = messagePoller;
        _handlerInvoker = handlerInvoker;
    }

    /// <inheritdoc/>
    public int ActiveMessageCount { get; set; }

    /// <inheritdoc/>
    public void StartProcessMessage(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping)
    {
        // TODO: a follow-up PR will handle managing this task, updating ActiveMessageCount, deleting the message when done.
        // This commit is just getting the HandlerInvoker in place
        var task = _handlerInvoker.InvokeAsync(messageEnvelope, subscriberMapping);
    }
}
