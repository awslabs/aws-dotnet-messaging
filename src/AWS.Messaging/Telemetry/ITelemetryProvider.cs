// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Telemetry;

/// <summary>
/// Provider for recording the telemetry emitted by AWS.Messaging library.
/// </summary>
public interface ITelemetryProvider
{
    /// <summary>
    /// Start a telemetry trace.
    /// </summary>
    /// <param name="traceName"></param>
    /// <returns></returns>
    ITelemetryTrace StartTrace(string traceName);

    /// <summary>
    /// Create a root trace for the start of processing a message.
    /// </summary>
    /// <param name="traceName"></param>
    /// <param name="envelope"></param>
    /// <returns></returns>
    ITelemetryTrace StartProcessMessageTrace(string traceName, MessageEnvelope envelope);
}
