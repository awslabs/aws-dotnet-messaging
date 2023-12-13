// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging;

namespace LambdaSubscriberApp
{
    public class TransactionHandler : IMessageHandler<TransactionInfo>
    {
        public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<TransactionInfo> messageEnvelope, CancellationToken token)
        {
            if (messageEnvelope == null)
            {
                return MessageProcessStatus.Failed();
            }

            if (messageEnvelope.Message == null)
            {
                return MessageProcessStatus.Failed();
            }

            var transactionInfo = messageEnvelope.Message;

            await Console.Out.WriteLineAsync($"Processed transaction ID = {transactionInfo.TransactionId} with amount = {transactionInfo.TransactionAmount}");

            return MessageProcessStatus.Success();
        }
    }
}
