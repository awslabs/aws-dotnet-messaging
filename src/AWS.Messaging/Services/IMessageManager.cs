// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// Instances of <see cref="AWS.Messaging.Services.IMessageManager" /> manage the lifecycle of a message being processed.
///
/// Responsibilities:
/// * Start the async work of processing messages and add the task to collection of active tasks
/// * Monitor the active processing message's task
/// * If a task completes with success status code delete message
/// * While the message tasks are working periodically inform the source the message is still being processed.
/// </summary>
public interface IMessageManager
{
    /// <summary>
    /// The number of active messages being processed.
    /// </summary>
    int ActiveMessageCount { get; }

    /// <summary>
    /// Start the async processing of a message.
    /// </summary>
    /// <param name="messageEnvelope">The message to start processing.</param>
    void StartProcessMessage(MessageEnvelope messageEnvelope);
}
