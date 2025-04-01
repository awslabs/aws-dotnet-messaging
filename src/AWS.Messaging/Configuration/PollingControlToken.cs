// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration
{
    /// <summary>
    /// Control token to start and stop message polling for a service.
    /// </summary>
    public class PollingControlToken
    {
        /// <summary>
        /// Indicates if polling is enabled.
        /// </summary>
        internal bool IsPollingEnabled { get; private set; } = true;

        /// <summary>
        /// Start polling of the SQS Queue.
        /// </summary>
        public void StartPolling() => IsPollingEnabled = true;

        /// <summary>
        /// Stop polling of the SQS Queue.
        /// </summary>
        public void StopPolling() => IsPollingEnabled = false;

        /// <summary>
        /// Configurable amount of time to wait between polling for a change in status
        /// </summary>
        public TimeSpan PollingWaitTime { get; init; } = TimeSpan.FromMilliseconds(200);
    }
}
