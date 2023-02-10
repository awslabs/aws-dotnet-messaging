// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

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
