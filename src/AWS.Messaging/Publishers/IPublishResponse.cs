// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers;

/// <summary>
/// Represents the results of a published message
/// </summary>
public interface IPublishResponse
{
    /// <summary>
    /// Gets and sets the property MessageId.
    /// <para>
    /// The ID of the message.
    /// </para>
    /// </summary>
    public string? MessageId { get; set; }
}
