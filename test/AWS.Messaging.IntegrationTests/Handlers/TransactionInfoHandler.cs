// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.IntegrationTests.Models;

namespace AWS.Messaging.IntegrationTests.Handlers;

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
        await Task.Delay(transactionInfo.WaitTime);

        if (_tempStorage.FifoMessages.TryGetValue(transactionInfo.UserId, out var messageGroup))
            messageGroup.Add(messageEnvelope);
        else
            _tempStorage.FifoMessages[transactionInfo.UserId] = new() { messageEnvelope };

        return await Task.FromResult(MessageProcessStatus.Success());
    }
}
