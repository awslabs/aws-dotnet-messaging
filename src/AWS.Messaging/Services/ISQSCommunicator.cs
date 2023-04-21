// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS.Model;

namespace AWS.Messaging.Services
{
    /// <summary>
    /// Can be used by an SQSMessagePoller to manage message lifecycle on SQS. Allows to swap out a pulling pattern for AWS Lambda.
    /// </summary>
    internal interface ISQSCommunicator
    {
        Task<List<Message>> ReceiveMessagesAsync(int numberOfMessagesToRead, CancellationToken token = default);

        Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default);

        Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default);

    }
}
