// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.EventBridge;
/// <summary>
/// Represents the results of an event published to an event bus.
/// <para>
/// If the publishing was successful, the entry has the event ID in it. Otherwise, an exception will be thrown
/// </para>
/// <para>
/// For information about the errors that are common to all actions, see <a href="https://docs.aws.amazon.com/eventbridge/latest/APIReference/CommonErrors.html">Common
/// Errors</a>.
/// </para>
/// </summary>
public class EventBridgePublishResponse : IPublishResponse
{
    /// <summary>
    /// Gets and sets the property MessageId.
    /// <para>
    /// The ID of the message.
    /// </para>
    /// </summary>
    public string? MessageId { get; set; }
}
