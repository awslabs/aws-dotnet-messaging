// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Telemetry
{
    /// <summary>
    /// Defines the kind of telemetry activity.
    /// </summary>
    public enum ActivityKind
    {
        /// <summary>
        /// Indicates the operation is producing a message (e.g., sending to SQS or SNS).
        /// </summary>
        Producer,

        /// <summary>
        /// Indicates the operation is consuming a message (e.g., receiving from SQS or EventBridge).
        /// </summary>
        Consumer,

        /// <summary>
        /// Indicates the operation is pulling messages from the queue.
        /// </summary>
        Server
    }
}
