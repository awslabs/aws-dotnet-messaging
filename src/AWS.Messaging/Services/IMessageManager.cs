// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;

namespace AWS.Messaging.Services;

/// <summary>
/// Instances of <see cref="AWS.Messaging.Services.IMessageManager" /> manage the lifecycle of a message being processed.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Start the async work of processing messages and add the task to collection of active tasks</description></item>
/// <item><description>Monitor the active processing message's task</description></item>
/// <item><description>If a task completes with success status code delete message</description></item>
/// <item><description>While the message tasks are working periodically inform the source the message is still being processed.</description></item>
/// </list>
/// </remarks>
public interface IMessageManager
{
    /// <summary>
    /// The number of active messages being processed.
    /// </summary>
    int ActiveMessageCount { get; }

    /// <summary>
    /// Allows a poller to wait for when <see cref="ActiveMessageCount"/> is next decremented or when the timeout elapses
    /// </summary>
    /// <param name="timeout">Maximum amount of time to wait</param>
    Task WaitAsync(TimeSpan timeout);

    /// <summary>
    /// Start the async processing of a message.
    /// </summary>
    /// <param name="messageEnvelope">The message to start processing</param>
    /// <param name="subscriberMapping">The mapping between the message's type and its handler</param>
    /// <param name="token">Optional token to cancel the message processing</param>
    Task ProcessMessageAsync(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping, CancellationToken token = default);

    /// <summary>
    /// Starts the async processing of messages within an ordered group.
    /// These messages are processed sequentially in the order in which they appear.
    /// </summary>
    /// <param name="messageGroup">The ordered list of messages that will be processed sequentially</param>
    /// <param name="groupId">The ID that uniquely identifies a message group</param>
    /// <param name="token">Optional token to cancel the message processing</param>
    Task ProcessMessageGroupAsync(List<ConvertToEnvelopeResult> messageGroup, string groupId, CancellationToken token = default);
}
