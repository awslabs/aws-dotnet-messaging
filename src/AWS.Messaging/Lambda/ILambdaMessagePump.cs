// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.SQSEvents;
using AWS.Messaging.Services;

namespace AWS.Messaging.Lambda;

/// <summary>
/// This interface provides the functionality to process SQS messages in a Lambda function.
/// </summary>
public interface ILambdaMessagePump
{
    /// <summary>
    /// Invokes the underlying <see cref="IMessagePoller"/> to process SQS messages. All messages that are successfull processed will be deleted from the SQS queue.
    /// </summary>
    /// <param name="sqsEvent">The <see cref="SQSEvent"/> object that contains the underlying SQS messages that will be processed</param>
    /// <param name="stoppingToken">Cancellation token that is passed into each poller</param>
    Task ExecuteAsync(SQSEvent sqsEvent, CancellationToken stoppingToken);

    /// <summary>
    /// Invokes the underlying <see cref="IMessagePoller"/> to process SQS messages. All messages that are failed to process will be included as part of <see cref="SQSBatchResponse"/>
    /// </summary>
    /// <param name="sqsEvent">The <see cref="SQSEvent"/> object that contains the underlying SQS messages that will be processed</param>
    /// <param name="stoppingToken">Cancellation token that is passed into each poller</param>
    Task<SQSBatchResponse> ExecuteWithSQSBatchResponseAsync(SQSEvent sqsEvent, CancellationToken stoppingToken);
}
