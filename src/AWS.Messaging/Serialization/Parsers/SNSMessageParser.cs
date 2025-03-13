// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization.Handlers;

namespace AWS.Messaging.Serialization.Parsers
{
    /// <summary>
    /// Parser for messages originating from Amazon Simple Notification Service (SNS).
    /// </summary>
    internal class SNSMessageParser : IMessageParser
    {
        /// <summary>
        /// Determines if the JSON element represents an SNS message by checking for required properties.
        /// </summary>
        /// <param name="root">The root JSON element to examine.</param>
        /// <returns>True if the message can be parsed as an SNS message; otherwise, false.</returns>
        public bool CanParse(JsonElement root)
        {
            return root.TryGetProperty("Type", out var type) &&
                   type.GetString() == "Notification" &&
                   root.TryGetProperty("MessageId", out _) &&
                   root.TryGetProperty("TopicArn", out _);
        }

        /// <summary>
        /// Parses an SNS message, extracting the inner message body and metadata.
        /// </summary>
        /// <param name="root">The root JSON element containing the SNS message.</param>
        /// <param name="originalMessage">The original SQS message.</param>
        /// <returns>A tuple containing the extracted message body and associated metadata.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the SNS message does not contain a valid Message property.</exception>
        public (string MessageBody, MessageMetadata Metadata) Parse(JsonElement root, Message originalMessage)
        {
            // Extract the inner message from the SNS wrapper
            var messageBody = root.GetProperty("Message").GetString()
                ?? throw new InvalidOperationException("SNS message does not contain a valid Message property");

            var metadata = new MessageMetadata
            {
                SNSMetadata = MessageMetadataHandler.CreateSNSMetadata(root)
            };

            return (messageBody, metadata);
        }
    }
}
