// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization;

namespace AWS.Messaging.Services;

/// <summary>
/// Contains helper methods to interact with Amazon SQS
/// </summary>
public interface ISQSHandler
{
    /// <summary>
    /// Retrieves messages from an SQS queue and converts them to a <see cref="MessageEnvelope"/>
    /// </summary>
    /// <param name="receiveMessageRequest">The request object that will be used by the underlying SQS client to retrieve messages.</param>
    /// <param name="token">The token used to signal the request cancellation</param>
    Task<List<ConvertToEnvelopeResult>> ReceiveMessageAsync(ReceiveMessageRequest receiveMessageRequest, CancellationToken token = default);

    /// <summary>
    /// Iterates over the messages enumerable and deletes them from the specified SQS queue URL
    /// </summary>
    /// <param name="messages">An enumerable of <see cref="MessageEnvelope"/> which wraps the underlying SQS message.</param>
    /// <param name="sqsQueueURL">The queue URL to delete messaged from.</param>
    /// <param name="token">The token used to signal the request cancellation</param>
    Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, string sqsQueueURL, CancellationToken token = default);

    /// <summary>
    /// Inform the underlying service to extend the message's visibility timeout because the message is still being processed.
    /// </summary>
    /// <param name="messages">The messages to extend their visibility timeout.</param>
    /// <param name="queueUrl">The SQS queue that stores the messages</param>
    /// <param name="visibilityTimeout">The time in seconds by which you want to extend the visibility timeout</param>
    /// <param name="token">Optional token to cancel the visibility timeout extension.</param>
    Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, string queueUrl, int visibilityTimeout, CancellationToken token = default);
}
