// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.EventBridge;
/// <summary>
/// Represents the results of an event published to an event bus.
///
///
/// <para>
/// If the publishing was successful, the entry has the event ID in it. Otherwise, you
/// can use the error code and error message to identify the problem with the entry.
/// </para>
///
/// <para>
/// For information about the errors that are common to all actions, see <a href="https://docs.aws.amazon.com/eventbridge/latest/APIReference/CommonErrors.html">Common
/// Errors</a>.
/// </para>
/// </summary>
public class EventBridgePublishResponse : IPublishResponse
{
    /// <summary>
        /// Gets and sets the property ErrorCode.
        /// <para>
        /// The error code that indicates why the event submission failed.
        /// </para>
        ///
        /// <para>
        /// Retryable errors include:
        /// </para>
        ///  <ul> <li>
        /// <para>
        ///  <code> <a href="https://docs.aws.amazon.com/eventbridge/latest/APIReference/CommonErrors.html">InternalFailure</a>
        /// </code>
        /// </para>
        ///
        /// <para>
        /// The request processing has failed because of an unknown error, exception or failure.
        /// </para>
        ///  </li> <li>
        /// <para>
        ///  <code> <a href="https://docs.aws.amazon.com/eventbridge/latest/APIReference/CommonErrors.html">ThrottlingException</a>
        /// </code>
        /// </para>
        ///
        /// <para>
        /// The request was denied due to request throttling.
        /// </para>
        ///  </li> </ul>
        /// <para>
        /// Non-retryable errors include:
        /// </para>
        ///  <ul> <li>
        /// <para>
        ///  <code> <a href="https://docs.aws.amazon.com/eventbridge/latest/APIReference/CommonErrors.html">AccessDeniedException</a>
        /// </code>
        /// </para>
        ///
        /// <para>
        /// You do not have sufficient access to perform this action.
        /// </para>
        ///  </li> <li>
        /// <para>
        ///  <code>InvalidAccountIdException</code>
        /// </para>
        ///
        /// <para>
        /// The account ID provided is not valid.
        /// </para>
        ///  </li> <li>
        /// <para>
        ///  <code>InvalidArgument</code>
        /// </para>
        ///
        /// <para>
        /// A specified parameter is not valid.
        /// </para>
        ///  </li> <li>
        /// <para>
        ///  <code>MalformedDetail</code>
        /// </para>
        ///
        /// <para>
        /// The JSON provided is not valid.
        /// </para>
        ///  </li> <li>
        /// <para>
        ///  <code>RedactionFailure</code>
        /// </para>
        ///
        /// <para>
        /// Redacting the CloudTrail event failed.
        /// </para>
        ///  </li> <li>
        /// <para>
        ///  <code>NotAuthorizedForSourceException</code>
        /// </para>
        ///
        /// <para>
        /// You do not have permissions to publish events with this source onto this event bus.
        /// </para>
        ///  </li> <li>
        /// <para>
        ///  <code>NotAuthorizedForDetailTypeException</code>
        /// </para>
        ///
        /// <para>
        /// You do not have permissions to publish events with this detail type onto this event
        /// bus.
        /// </para>
        ///  </li> </ul>
        /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets and sets the property ErrorMessage.
    /// <para>
    /// The error message that explains why the event submission failed.
    /// </para>
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets and sets the property EventId.
    /// <para>
    /// The ID of the event.
    /// </para>
    /// </summary>
    public string? MessageId { get; set; }
}
