// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SimpleNotificationService.Model;
using AWS.Messaging.Services;

namespace AWS.Messaging.Publishers.SNS
{
    /// <summary>
    /// This interface allows publishing messages from application code to Amazon SNS.
    /// It exposes the <see cref="PublishAsync{T}(T, SNSOptions?, CancellationToken)"/> method which takes in a user-defined message, and <see cref="SNSOptions"/> to set additonal parameters while publishing messages to SNS.
    /// Using dependency injection, this interface is available to inject anywhere in the code.
    /// </summary>
    public interface ISNSPublisher : IEventPublisher
    {
        /// <summary>
        /// Publishes the application message to SNS.
        /// </summary>
        /// <param name="message">The application message that will be serialized and sent to an SNS topic</param>
        /// <param name="snsOptions">Contains additional parameters that can be set while sending a message to an SNS topic</param>
        /// <param name="token">The cancellation token used to cancel the request.</param>
        Task<SNSPublishResponse> PublishAsync<T>(T message, SNSOptions? snsOptions, CancellationToken token = default);
    }
}
