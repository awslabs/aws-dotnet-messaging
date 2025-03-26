// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0


namespace AWS.Messaging
{
    /// <summary>
    /// Contains Metadata related to Amazon EventBridge.
    /// </summary>
    public class EventBridgeMetadata
    {
        /// <summary>
        /// The unique event identifier
        /// </summary>
        public string? EventId { get; set; }

        /// <summary>
        /// The type of the event that was sent.
        /// </summary>
        public string? DetailType { get; set; }

        /// <summary>
        /// Identifies the source of the event
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// The time the event occurred.
        /// </summary>
        public DateTimeOffset Time { get; set; }

        /// <summary>
        /// The 12-digit number identifying an AWS account that published the event.
        /// </summary>
        public string? AWSAccount { get; set; }

        /// <summary>
        /// Identifies the AWS Region where the event originated.
        /// </summary>
        public string? AWSRegion { get; set; }

        /// <summary>
        /// Contains a list of Amazon Resource Names that the event primarily concerns.
        /// </summary>
        public List<string>? Resources { get; set; }
    }
}
