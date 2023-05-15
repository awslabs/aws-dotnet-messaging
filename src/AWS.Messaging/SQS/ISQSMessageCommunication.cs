// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Messaging.SQS;

/// <summary>
/// Provides APIs for the IMessageManager to communicate back to SQS the status of a Message.
/// </summary>
public interface ISQSMessageCommunication
{
    /// <summary>
    /// Indicates whether extending message visibility is supported.
    /// </summary>
    bool SupportExtendingVisibilityTimeout { get; }

    /// <summary>
    /// How frequently message visibility should be extended in seconds
    /// via <see cref="ExtendMessageVisibilityTimeoutAsync"/> while the message is still being processed
    /// </summary>
    int VisibilityTimeoutExtensionInterval { get; }

    /// <summary>
    /// Report back to the communication implementer when a message .failed to be processed.
    /// </summary>
    /// <param name="message">The <see cref="MessageEnvelope"/> that was not processed correctly.</param>
    ValueTask ReportMessageFailureAsync(MessageEnvelope message);

    /// <summary>
    /// Delete the message in the underlying service.
    /// </summary>
    /// <param name="messages">The messages to delete.</param>
    /// <param name="token">Optional token to cancel the deletion.</param>
    Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default);

    /// <summary>
    /// Inform the underlying service to extend the message's visibility timeout because the message is still being processed.
    /// </summary>
    /// <param name="messages">The messages to extend their visibility timeout.</param>
    /// <param name="token">Optional token to cancel the visibility timeout extension.</param>
    Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default);
}
