// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Publishers.EventBridge
{
    /// <summary>
    /// This class contains additional properties that can be set while publishing a message to Amazon EventBridge.
    /// </summary>
    public class EventBridgeOptions
    {
        /// <summary>
        /// The source of the event.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// The time stamp of the event, per RFC3339. If no time stamp is provided, the time stamp of the PutEvents call is used.
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// An X-Ray trace header, which is an http header(X-Amzn-Trace-Id) that contains the trace-id associated with the event.
        /// To learn more about X-Ray trace headers, see <see href="https://docs.aws.amazon.com/xray/latest/devguide/xray-concepts.html#xray-concepts-tracingheader">Tracing header</see> in the X-Ray Developer Guide.
        /// </summary>
        public string? TraceHeader { get; set; }
    }
}
