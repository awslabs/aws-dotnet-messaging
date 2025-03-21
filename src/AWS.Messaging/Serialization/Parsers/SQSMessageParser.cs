// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization.Handlers;

namespace AWS.Messaging.Serialization.Parsers;

/// <summary>
/// Default fallback parser for Amazon Simple Queue Service (SQS) messages.
/// This parser handles messages that don't match other specialized parsers.
/// </summary>
internal class SQSMessageParser : IMessageParser
{
    /// <summary>
    /// Always returns true as this is the default fallback parser for any message format.
    /// </summary>
    /// <param name="_">The root JSON element (unused in this implementation).</param>
    /// <returns>Always returns true, indicating this parser can handle any remaining message format.</returns>
    public bool CanParse(JsonElement _) => true; // Default fallback parser

    /// <summary>
    /// Parses an SQS message, preserving the original message body and adding SQS metadata.
    /// </summary>
    /// <param name="root">The root JSON element containing the message content.</param>
    /// <param name="originalMessage">The original SQS message containing metadata information.</param>
    /// <returns>A tuple containing the unchanged message body and associated SQS metadata.</returns>
    public (string MessageBody, MessageMetadata Metadata) Parse(JsonElement root, Message originalMessage)
    {
        var metadata = new MessageMetadata
        {
            SQSMetadata = MessageMetadataHandler.CreateSQSMetadata(originalMessage)
        };

        // Return the raw message without modification since this is the base parser
        return (root.GetRawText(), metadata);
    }
}
