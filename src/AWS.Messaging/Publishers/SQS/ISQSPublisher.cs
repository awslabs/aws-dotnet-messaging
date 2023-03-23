// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.SQS
{
    /// <summary>
    /// This interface allows publishing messages from application code to Amazon SQS.
    /// It exposes the <see cref="PublishAsync{T}(T, SQSOptions?, CancellationToken)"/> method which takes in a user-defined message, and <see cref="SQSOptions"/> to set additonal parameters while publishing messages to SQS.
    /// Using dependency injection, this interface is available to inject anywhere in the code.
    /// </summary>
    public interface ISQSPublisher
    {
        /// <summary>
        /// Publishes the application message to SQS.
        /// </summary>
        /// <param name="message">The application message that will be serialized and sent to an SQS queue</param>
        /// <param name="sqsOptions">Contains additional parameters that can be set while sending a message to an SQS queue</param>
        /// <param name="token">The cancellation token used to cancel the request.</param>
        Task PublishAsync<T>(T message, SQSOptions? sqsOptions, CancellationToken token = default);
    }
}
