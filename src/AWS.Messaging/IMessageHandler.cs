// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging;

/// <summary>
/// This interface is implemented by the users of this library for each type of message that should be processed.
///
/// The implementation of this interface is where the business logic for processing a particular message type is written.
/// The message type is indicated by the generic type T for the interface. The library will call the handler whenever
/// the message type is read from the message source.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMessageHandler<T>
{
    /// <summary>
    /// This method is where the business logic for processing the message will start.
    ///
    /// If the message was successfully processed and should them <see cref="MessageProcessStatus.Success()"/> should be returned. The
    /// underlying service like SQS will be told the message has been processed and can be removed.
    ///
    /// If an exception is thrown from this method the status is treated as <see cref="MessageProcessStatus.Failed()"/>.
    /// </summary>
    /// <param name="messageEnvelope">The message read from the message source wrapped around a message envelope containing message metadata.</param>
    /// <param name="token">The optional cancellation token.</param>
    /// <returns>The status of the processed message. For example whether the message was successfully processed.</returns>
    Task<MessageProcessStatus> HandleAsync(MessageEnvelope<T> messageEnvelope, CancellationToken token = default(CancellationToken));
}
