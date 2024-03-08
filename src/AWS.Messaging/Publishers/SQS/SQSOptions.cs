// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;

namespace AWS.Messaging.Publishers.SQS
{
    /// <summary>
    /// This class contains additional properties that can be set while publishing a message to an SQS queue.
    /// </summary>
    public class SQSOptions
    {
        /// <summary>
        /// The length of time, in seconds, for which to delay a specific message.
        /// Its valid values are between 0 to 900.
        /// Messages with a positive DelaySeconds value become available for processing after the delay period is finished. If you don't specify a value, the default value for the queue applies.
        /// When you set FifoQueue, you can't set DelaySeconds per message. You can set this parameter only on a queue level.
        /// </summary>
        public int? DelaySeconds { get; set; }

        /// <summary>
        /// Each message attribute consists of a Name, Type, and Value.
        /// For more information, see <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-message-metadata.html#sqs-message-attributes">the Amazon SQS developer guide.</see>
        /// </summary>
        public Dictionary<string, MessageAttributeValue>? MessageAttributes { get; set; }

        /// <summary>
        /// This parameter applies only to FIFO(first-in-first-out) queues and is used for deduplication of sent messages.
        /// If a message with a particular MessageDeduplicationId is sent successfully, any messages sent with the same MessageDeduplicationId are accepted successfully but aren't delivered during the 5-minute deduplication interval.
        /// For more information, see <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/FIFO-queues-exactly-once-processing.html">Exactly-once processing</see> in the Amazon SQS Developer Guide.
        /// If the queue has ContentBasedDeduplication enabled, the system generates a MessageDeduplicationId based on the contents of the message.
        /// If ContentBasedDeduplication is disabled, then the MessageDeduplicationId must be provided explicitly.
        /// The maximum length of MessageDeduplicationId is 128 characters and it can contain alphanumeric characters (a-z, A-Z, 0-9) and punctuation (<code>!"#$%&amp;'()*+,-./:;&lt;=&gt;?@[\]^_`{|}~</code>).
        /// Refer to the <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/using-messagededuplicationid-property.html">SQS developer guide</see> for best practicies while using MessageDeduplicationId.
        /// </summary>
        public string? MessageDeduplicationId { get; set; }

        /// <summary>
        /// This parameter applies only to FIFO(first-in-first-out) queues and specifies that a message belongs to a specific message group.
        /// Messages that belong to the same message group are processed in a FIFO manner(however, messages in different message groups might be processed out of order).
        /// To interleave multiple ordered streams within a single queue, use MessageGroupId values(for example, session data for multiple users).
        /// In this scenario, multiple consumers can process the queue, but the session data of each user is processed in a FIFO fashion.
        /// The maximum length of MessageGroupId is 128 characters and it can contain alphanumeric characters (a-z, A-Z, 0-9) and punctuation (<code>!"#$%&amp;'()*+,-./:;&lt;=&gt;?@[\]^_`{|}~</code>).
        /// Refer to the <see href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/using-messagegroupid-property.html">SQS developer guide</see> for best practices while using MessageGroupId.
        /// </summary>
        public string? MessageGroupId { get; set; }

        /// <summary>
        /// The SQS queue URL which the publisher will use to route the message. This can be used to override the queue URL
        /// that is configured for a given message type when publishing a specific message.
        /// </summary>
        public string? QueueUrl { get; set; }

        /// <summary>
        /// An alternative SQS client that can be used to publish a specific message,
        /// instead of the client provided by the registered <see cref="IAWSClientProvider"/> implementation.
        /// </summary>
        public IAmazonSQS? OverrideClient { get; set; }
    }
}
