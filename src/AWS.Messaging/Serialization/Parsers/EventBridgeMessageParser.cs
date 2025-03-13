// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization.Handlers;

namespace AWS.Messaging.Serialization.Parsers
{
    /// <summary>
    /// Parser for messages originating from Amazon EventBridge.
    /// </summary>
    internal class EventBridgeMessageParser : IMessageParser
    {
        /// <summary>
        /// Determines if the JSON element represents an EventBridge message by checking for required properties.
        /// </summary>
        /// <param name="root">The root JSON element to examine.</param>
        /// <returns>True if the message can be parsed as an EventBridge message; otherwise, false.</returns>
        public bool CanParse(JsonElement root)
        {
            return root.TryGetProperty("detail", out _) &&
                   root.TryGetProperty("detail-type", out _) &&
                   root.TryGetProperty("source", out _) &&
                   root.TryGetProperty("time", out _);
        }

        /// <summary>
        /// Parses an EventBridge message, extracting the message body and metadata.
        /// </summary>
        /// <param name="root">The root JSON element containing the EventBridge message.</param>
        /// <param name="originalMessage">The original SQS message.</param>
        /// <returns>A tuple containing the message body and associated metadata.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the EventBridge message does not contain a valid detail property.</exception>
        public (string MessageBody, MessageMetadata Metadata) Parse(JsonElement root, Message originalMessage)
        {
            // The detail property can be either a string or an object
            var detailElement = root.GetProperty("detail");

            // Add explicit check for null detail
            if (detailElement.ValueKind == JsonValueKind.Null)
            {
                throw new InvalidOperationException("EventBridge message does not contain a valid detail property");
            }

            var messageBody = detailElement.ValueKind == JsonValueKind.String
                ? detailElement.GetString()
                : detailElement.GetRawText();

            if (string.IsNullOrEmpty(messageBody))
            {
                throw new InvalidOperationException("EventBridge message does not contain a valid detail property");
            }

            var metadata = new MessageMetadata
            {
                EventBridgeMetadata = MessageMetadataHandler.CreateEventBridgeMetadata(root)
            };

            return (messageBody, metadata);
        }
    }
}
