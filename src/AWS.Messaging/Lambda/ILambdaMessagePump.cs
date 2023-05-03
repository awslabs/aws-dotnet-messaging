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
    /// Invokes the underlying <see cref="IMessagePoller"/> to process SQS messages. All messages that are successfully processed will be deleted from the SQS queue.
    /// Use this method when your SQS event source mapping is not configured to use <see href="https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html#services-sqs-batchfailurereporting">partial batch responses.</see>
    /// </summary>
    /// <param name="sqsEvent">The <see cref="SQSEvent"/> object that contains the underlying SQS messages that will be processed.</param>
    /// <param name="stoppingToken">Cancellation token that is passed into each poller.</param>
    /// <param name="options">Configuration options for polling and processing messages from SQS.</param>
    Task ExecuteAsync(SQSEvent sqsEvent, CancellationToken stoppingToken, LambdaMessagePollerOptions? options = null);

    /// <summary>
    /// Invokes the underlying <see cref="IMessagePoller"/> to process SQS messages. All messages that are failed to process will be included as part of <see cref="SQSBatchResponse"/>.
    /// Use this method when your SQS event source mapping is configured to use <see href="https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html#services-sqs-batchfailurereporting">partial batch responses.</see>
    /// </summary>
    /// <param name="sqsEvent">The <see cref="SQSEvent"/> object that contains the underlying SQS messages that will be processed.</param>
    /// <param name="stoppingToken">Cancellation token that is passed into each poller.</param>
    /// <param name="options">Configuration options for polling and processing messages from SQS.</param>
    Task<SQSBatchResponse> ExecuteWithSQSBatchResponseAsync(SQSEvent sqsEvent, CancellationToken stoppingToken, LambdaMessagePollerOptions? options = null);
}
