// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging;

/// <summary>
/// Stores the configuration to facilitate the creation of <see cref="MessageEnvelope"/>
/// </summary>
internal class MessageEnvelopeConfiguration
{
    /// <summary>
    /// Stores the JSON blob that will be deserialized into <see cref="MessageEnvelope"/>
    /// </summary>
    public string MessageEnvelopeBody { get; init; }

    /// <summary>
    /// Stores metadata related to Amazon SQS.
    /// </summary>
    public SQSMetadata SQSMetadata { get; init; }

    /// <summary>
    /// Stores metadata related to Amazon SNS.
    /// </summary>
    public SNSMetadata SNSMetadata { get; init; }

    /// <summary>
    /// Creates an instance of <see cref="MessageEnvelopeConfiguration"/>
    /// </summary>
    /// <param name="messageEnvelopeBody">Stores the JSON blob that will be deserialized into <see cref="MessageEnvelope"/></param>
    /// <param name="sqsMetadata">Stores metadata related to Amazon SQS.</param>
    /// <param name="snsMetadata">Stores metadata related to Amazon SNS.</param>
    public MessageEnvelopeConfiguration(string messageEnvelopeBody, SQSMetadata sqsMetadata, SNSMetadata snsMetadata)
    {
        MessageEnvelopeBody = messageEnvelopeBody;
        SQSMetadata = sqsMetadata;
        SNSMetadata = snsMetadata;
    }
}
