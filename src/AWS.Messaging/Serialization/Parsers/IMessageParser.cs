// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization;

namespace AWS.Messaging.Serialization.Parsers;

/// <summary>
/// Defines the contract for message parsers capable of handling different message formats.
/// </summary>
internal interface IMessageParser
{
    /// <summary>
    /// Determines if the parser can handle the given JSON element.
    /// </summary>
    /// <param name="root">The root JSON element to examine.</param>
    /// <returns>True if the parser can handle the message; otherwise, false.</returns>
    bool CanParse(JsonElement root);

    /// <summary>
    /// Parses the message, extracting the message body and associated metadata.
    /// </summary>
    /// <param name="root">The root JSON element containing the message to parse.</param>
    /// <param name="originalMessage">The original SQS message.</param>
    /// <returns>A tuple containing the extracted message body and associated metadata.</returns>
    (string MessageBody, MessageMetadata Metadata) Parse(JsonElement root, Message originalMessage);
}
