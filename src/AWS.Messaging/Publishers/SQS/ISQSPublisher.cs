// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Services;

namespace AWS.Messaging.Publishers.SQS
{
    /// <summary>
    /// This interface allows sending messages from application code to Amazon SQS.
    /// It exposes the <see cref="SendAsync{T}(T, SQSOptions?, CancellationToken)"/> method which takes in a user-defined message, and <see cref="SQSOptions"/> to set additional parameters while sending messages to SQS.
    /// Using dependency injection, this interface is available to inject anywhere in the code.
    /// </summary>
    public interface ISQSPublisher : ICommandPublisher
    {
        /// <summary>
        /// Sends the application message to SQS.
        /// </summary>
        /// <param name="message">The application message that will be serialized and sent to an SQS queue</param>
        /// <param name="sqsOptions">Contains additional parameters that can be set while sending a message to an SQS queue</param>
        /// <param name="token">The cancellation token used to cancel the request.</param>
        Task<SQSSendResponse> SendAsync<T>(T message, SQSOptions? sqsOptions, CancellationToken token = default);
    }
}
