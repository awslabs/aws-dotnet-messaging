// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers;

/// <summary>
/// Represents the results of a published event
/// </summary>
public interface IPublishResponse
{
    /// <summary>
    /// Gets and sets the property ErrorMessage.
    /// <para>
    /// The error message that explains why the event publish failed.
    /// </para>
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets and sets the property EventId.
    /// <para>
    /// The ID of the event.
    /// </para>
    /// </summary>
    public string? EventId { get; set; }
}
