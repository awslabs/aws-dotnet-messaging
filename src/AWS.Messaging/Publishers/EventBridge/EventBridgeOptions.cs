// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.EventBridge;
using AWS.Messaging.Configuration;

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

        /// <summary>
        /// Specifies the type of event being sent.
        /// </summary>
        public string? DetailType { get; set; }

        /// <summary>
        /// Contains a list of Amazon Resource Names that the event primarily concerns.
        /// </summary>
        public List<string>? Resources { get; set; }

        /// <summary>
        /// The EventBridge Event Bus name or ARN which the publisher will use to route the message.
        /// This can be used to override the EventBusName that is configured for a given message
        /// type when publishing a specific message.
        /// </summary>
        public string? EventBusName { get; set; }

        /// <summary>
        /// The ID of the global EventBridge endpoint. This can be used to override the EndpointID
        /// that is configured for a given message type when publishing a specific message.
        /// </summary>
        public string? EndpointID { get; set; }

        /// <summary>
        /// An alternative EventBridge client that can be used to publish a specific message,
        /// instead of the client provided by the registered <see cref="IAWSClientProvider"/> implementation.
        /// </summary>
        public IAmazonEventBridge? OverrideClient { get; set; }
    }
}
