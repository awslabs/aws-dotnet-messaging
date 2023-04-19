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
    public string? MessageEnvelopeBody { get; set; }

    /// <summary>
    /// Stores metadata related to Amazon SQS.
    /// </summary>
    public SQSMetadata? SQSMetadata { get; set; }

    /// <summary>
    /// Stores metadata related to Amazon SNS.
    /// </summary>
    public SNSMetadata? SNSMetadata { get; set; }

    /// <summary>
    /// Stores metadata related to Amazon EventBridge.
    /// </summary>
    public EventBridgeMetadata? EventBridgeMetadata { get; set; }
}
