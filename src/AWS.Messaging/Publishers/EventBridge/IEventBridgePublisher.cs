// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.EventBridge
{
    /// <summary>
    /// This interface allows publishing messages from application code to Amazon EventBridge.
    /// It exposes the <see cref="PublishAsync{T}(T, EventBridgeOptions?, CancellationToken)"/> method which takes in a user-defined message, and <see cref="EventBridgeOptions"/> to set additonal parameters while publishing messages to EventBridge.
    /// Using dependency injection, this interface is available to inject anywhere in the code.
    /// </summary>
    public interface IEventBridgePublisher
    {
        /// <summary>
        /// Publishes the application message to SNS.
        /// </summary>
        /// <param name="message">The application message that will be serialized and sent to an SNS topic</param>
        /// <param name="eventBridgeOptions">Contains additional parameters that can be set while sending a message to EventBridge</param>
        /// <param name="token">The cancellation token used to cancel the request.</param>
        Task PublishAsync<T>(T message, EventBridgeOptions? eventBridgeOptions, CancellationToken token = default);
    }
}
