// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.SQS;

/// <summary>
/// The response for an SQS Send action.
/// </summary>
public class SQSSendResponse : IPublishResponse
{

    /// <summary>
    /// Gets and sets the property MessageId.
    /// <para>
    /// An attribute containing the <code>MessageId</code> of the message sent to the queue.
    /// For more information, see <a href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-queue-message-identifiers.html">Queue
    /// and Message Identifiers</a> in the <i>Amazon SQS Developer Guide</i>.
    /// </para>
    /// </summary>
    public string? MessageId { get; set; }
}
