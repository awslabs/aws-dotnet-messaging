// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Services;

namespace AWS.Messaging.Configuration
{
    /// <summary>
    /// Internal configuration for a <see cref="DefaultMessageManager"/>
    /// </summary>
    /// <remarks>
    /// Currently this closely mirrors <see cref="SQSMessagePollerConfiguration"/>, but could be expanded
    /// if we allow message managers to be configured independently of their poller(s)
    /// </remarks>
    public class MessageManagerConfiguration
    {
        /// <inheritdoc cref="SQSMessagePollerConfiguration.VisibilityTimeoutExtensionThreshold"/>
        internal int VisibilityTimeoutExtensionThreshold { get; set; } = SQSMessagePollerConfiguration.DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_THRESHOLD_SECONDS;

        /// <inheritdoc cref="SQSMessagePollerConfiguration.VisibilityTimeoutExtensionHeartbeatInterval"/>
        internal TimeSpan VisibilityTimeoutExtensionHeartbeatInterval { get; set; } = SQSMessagePollerConfiguration.DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_HEARTBEAT_INTERVAL;

    }
}
