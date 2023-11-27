// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Tests.Common.Models;

namespace AWS.Messaging.Tests.Common.Handlers;

public class TransactionInfoHandler : IMessageHandler<TransactionInfo>
{
    private readonly TempStorage<TransactionInfo> _tempStorage;

    public TransactionInfoHandler(TempStorage<TransactionInfo> tempStorage)
    {
        _tempStorage = tempStorage;
    }

    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<TransactionInfo> messageEnvelope, CancellationToken token = default)
    {
        var transactionInfo = messageEnvelope.Message;
        await Task.Delay(transactionInfo.WaitTime, token);

        if (messageEnvelope.Message.ShouldFail)
        {
            return await Task.FromResult(MessageProcessStatus.Failed());
        }

        if (_tempStorage.FifoMessages.TryGetValue(transactionInfo.UserId, out var messageGroup))
            messageGroup.Add(messageEnvelope);
        else
            _tempStorage.FifoMessages[transactionInfo.UserId] = new() { messageEnvelope };

        return await Task.FromResult(MessageProcessStatus.Success());
    }
}
