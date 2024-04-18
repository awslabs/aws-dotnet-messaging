// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.SNS;

/// <summary>
/// The response for an SNS publish action.
/// </summary>
public class SNSPublishResponse : IPublishResponse
{
    /// <summary>
    /// Gets and sets the property MessageId.
    /// <para>
    /// Unique identifier assigned to the published message.
    /// </para>
    ///
    /// <para>
    /// Length Constraint: Maximum 100 characters
    /// </para>
    /// </summary>
    public string? MessageId { get; set; }
}
