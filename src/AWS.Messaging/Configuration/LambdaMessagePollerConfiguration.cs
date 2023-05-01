// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
using Amazon.Lambda.SQSEvents;

namespace AWS.Messaging.Configuration;

/// <summary>
/// Internal configuration  while working with AWS Lambda to process messages from SQS
/// </summary>
internal class LambdaMessagePollerConfiguration : IMessagePollerConfiguration
{
    /// This can be used as the return type for Lambda functions that have partially
    /// succeeded by supplying a list of message IDs that have failed to process.
    /// https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html#services-sqs-batchfailurereporting
    public SQSBatchResponse? SQSBatchResponse { get; init; }

    /// <summary>
    /// The SQS event that will be processed by the Lambda function.
    /// </summary>
    public SQSEvent? SQSEvent { get; init; }

    /// <summary>
    /// The SQS queue which acts as an event trigger for the Lambda function.
    /// </summary>
    public string SubscriberEndpoint { get; init; }

    /// <summary>
    /// Creates an instance of <see cref="LambdaMessagePollerConfiguration"/>
    /// </summary>
    /// <param name="queueUrl">The SQS queue which acts as an event trigger for the Lambda function.</param>
    public LambdaMessagePollerConfiguration(string queueUrl)
    {
        SubscriberEndpoint = queueUrl;
    }
}
