// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AWS.Messaging.Configuration;

namespace AWS.Messaging.Publishers.SNS
{
    /// <summary>
    /// This class contains additional properties that can be set while publishing a message to an SNS topic.
    /// </summary>
    public class SNSOptions
    {
        /// <summary>
        /// Each message attribute consists of a Name, Type, and Value.
        /// For more information, see <see href="https://docs.aws.amazon.com/sns/latest/dg/sns-message-attributes.html">the Amazon SNS developer guide.</see>
        /// </summary>
        public Dictionary<string, MessageAttributeValue>? MessageAttributes { get; set; }

        /// <summary>
        /// This parameter applies only to FIFO(first-in-first-out) topics and can contain up to 128 alphanumeric characters (a-z, A-Z, 0-9) and punctuation (<code>!"#$%&amp;'()*+,-./:;&lt;=&gt;?@[\]^_`{|}~</code>).
        /// Every message must have a unique MessageDeduplicationId, which is a token used for deduplication of sent messages.
        /// If a message with a particular MessageDeduplicationId is sent successfully, any message sent with the same MessageDeduplicationId during the 5-minute deduplication interval is treated as a duplicate.
        /// If the topic has ContentBasedDeduplication enabled, the system generates a MessageDeduplicationId based on the contents of the message.
        /// If ContentBasedDeduplication is disabled, then the value of MessageDeduplicationId must be provided explicitly.
        /// Your MessageDeduplicationId overrides the generated one.
        /// </summary>
        public string? MessageDeduplicationId { get; set; }

        /// <summary>
        /// This parameter applies only to FIFO(first-in-first-out) topics and can contain up to 128 alphanumeric characters(a-z, A-Z, 0-9) and punctuation (<code>!"#$%&amp;'()*+,-./:;&lt;=&gt;?@[\]^_`{|}~</code>).
        /// The MessageGroupId is a tag that specifies that a message belongs to a specific message group.
        /// Messages that belong to the same message group are processed in a FIFO manner (however, messages in different message groups might be processed out of order).
        /// Every message must include a MessageGroupId.
        /// </summary>
        public string? MessageGroupId { get; set; }

        /// <summary>
        /// Subjects must be ASCII text that begins with a letter, number, or punctuation mark and must not include line breaks or control characters.
        /// It must be less than 100 characters long.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// The SNS Topic Arn
        /// </summary>
        public string? TopicArn { get; set; }

        /// <summary>
        /// An alternative SNS client that can be used to publish a specific message,
        /// instead of the client provided by the registered <see cref="IAWSClientProvider"/> implementation.
        /// </summary>
        public IAmazonSimpleNotificationService? OverrideClient { get; set; }
    }
}
