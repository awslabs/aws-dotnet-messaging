// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Serialization;

/// <summary>
/// Represents the results of a message serialization operation, containing both the serialized data
/// and its corresponding content type.
/// </summary>
public class MessageSerializerResults
{
    /// <summary>
    /// Initializes a new instance of the MessageSerializerResults class.
    /// </summary>
    /// <param name="data">The serialized message data as a string.</param>
    /// <param name="contentType">The MIME content type of the serialized data.</param>
    public MessageSerializerResults(string data, string contentType)
    {
        Data = data;
        ContentType = contentType;
    }

    /// <summary>
    /// Gets or sets the MIME content type of the serialized data.
    /// Common values include "application/json" or "application/xml".
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets or sets the serialized message data as a string.
    /// This contains the actual serialized content of the message.
    /// </summary>
    public string Data { get; }
}
