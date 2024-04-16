// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.SNS;

/// <summary>
/// Response for Publish action.
/// </summary>
public class SNSPublishResponse : IPublishResponse
{
    /// <summary>
    /// The error message as provided by SNS, if any
    /// </summary>
    public string? ErrorMessage { get; set; }

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
    public string? EventId { get; set; }
}
