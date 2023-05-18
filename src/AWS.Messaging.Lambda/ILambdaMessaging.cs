// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Messaging.Services;

namespace AWS.Messaging.Lambda;

/// <summary>
/// This interface provides the functionality to process SQS messages in a Lambda function. This interface is what Lambda functions must retrieve from the <see cref="IServiceProvider"/> and
/// invoke one of the process methods passing the <see cref="Amazon.Lambda.SQSEvents.SQSEvent"/> and <see cref="Amazon.Lambda.Core.ILambdaContext"/>.
/// </summary>
public interface ILambdaMessaging
{
    /// <summary>
    /// Initiates processing of SQS messages to the Lambda function. All messages that are successfully processed will be deleted from the SQS queue.
    /// Use this method when your SQS event source mapping is not configured to use <see href="https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html#services-sqs-batchfailurereporting">partial batch responses.</see>
    /// </summary>
    /// <param name="sqsEvent">The <see cref="SQSEvent"/> object that contains the underlying SQS messages that will be processed.</param>
    /// <param name="lambdaContext">The ILambdaContext for the function invocation.</param>
    Task ProcessLambdaEventAsync(SQSEvent sqsEvent, ILambdaContext lambdaContext);

    /// <summary>
    /// Initiates processing of SQS messages to the Lambda function. All messages that are failed to process will be included as part of <see cref="SQSBatchResponse"/>.
    /// Use this method when your SQS event source mapping is configured to use <see href="https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html#services-sqs-batchfailurereporting">partial batch responses.</see>
    /// </summary>
    /// <param name="sqsEvent">The <see cref="SQSEvent"/> object that contains the underlying SQS messages that will be processed.</param>
    /// <param name="lambdaContext">The ILambdaContext for the function invocation.</param>
    Task<SQSBatchResponse> ProcessLambdaEventWithBatchResponseAsync(SQSEvent sqsEvent, ILambdaContext lambdaContext);
}
