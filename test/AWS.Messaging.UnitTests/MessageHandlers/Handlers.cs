// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.UnitTests.Models;

namespace AWS.Messaging.UnitTests.MessageHandlers;

public class ChatMessageHandler : IMessageHandler<ChatMessage>
{
    public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ChatMessage> messageEnvelope, CancellationToken token = default)
    {
        return Task.FromResult(MessageProcessStatus.Success());
    }
}

public class AddressInfoHandler : IMessageHandler<AddressInfo>
{
    public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<AddressInfo> messageEnvelope, CancellationToken token = default)
    {
        return Task.FromResult(MessageProcessStatus.Success());
    }
}

/// <summary>
/// Implements handling for mutiple message types
/// </summary>
public class DualHandler : IMessageHandler<ChatMessage>, IMessageHandler<AddressInfo>
{
    public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ChatMessage> messageEnvelope, CancellationToken token = default)
    {
        return Task.FromResult(MessageProcessStatus.Success());
    }

    public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<AddressInfo> messageEnvelope, CancellationToken token = default)
    {
        return Task.FromResult(MessageProcessStatus.Failed());
    }
}

/// <summary>
/// Custom exception to throw from a handler
/// </summary>
public class CustomHandlerException : Exception
{
    public CustomHandlerException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Always throws a custom exception, useful for testing error handling
/// </summary>
public class ChatExceptionHandler : IMessageHandler<ChatMessage>
{
    public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ChatMessage> messageEnvelope, CancellationToken token = default)
    {
        throw new CustomHandlerException($"Unable to process message {messageEnvelope.Id}");
    }
}
