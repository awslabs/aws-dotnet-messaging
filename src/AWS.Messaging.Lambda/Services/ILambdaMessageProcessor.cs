// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.SQSEvents;

namespace AWS.Messaging.Lambda.Services;

/// <summary>
/// The implementation of this interface is created from the <see cref="ILambdaMessageProcessorFactory"/>. There will be
/// one instance of this interface per Lambda invocation.
/// </summary>
public interface ILambdaMessageProcessor
{
    /// <summary>
    /// Initiates the processing of all the messages for a Lambda invocation
    /// and if partial failure is enabled returns a <see cref="Amazon.Lambda.SQSEvents.SQSBatchResponse"/>.
    /// </summary>
    /// <param name="token"></param>
    /// <returns>If partial failure is enabled then a response of the messages that failed to process.</returns>
    Task<SQSBatchResponse?> ProcessMessagesAsync(CancellationToken token = default);
}
