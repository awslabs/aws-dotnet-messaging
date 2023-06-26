// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Telemetry;

/// <summary>
/// Service interface the AWS.Messaging library uses to start telemetry traces.
/// </summary>
public interface ITelemetryFactory
{
    /// <summary>
    /// Boolean indicating if telemetry is enabled.
    /// </summary>
    bool IsTelemetryEnabled { get; }

    /// <summary>
    /// Start a trace represented by the returned <see cref="ITelemetryTrace"/>. The trace will end when the <see cref="ITelemetryTrace"/> is disposed.
    /// </summary>
    /// <param name="traceName">The name of the trace.</param>
    /// <returns>The state of the trace.</returns>
    ITelemetryTrace Trace(string traceName);

    /// <summary>
    /// Start a trace represented by the returned <see cref="ITelemetryTrace"/>. The trace will end when the <see cref="ITelemetryTrace"/> is disposed.
    /// The <see cref="MessageEnvelope"/> is used to look for parent trace metadata to connect traces from publishers to subscribers.
    /// </summary>
    /// <param name="traceName"></param>
    /// <param name="envelope"></param>
    /// <returns></returns>
    ITelemetryTrace Trace(string traceName, MessageEnvelope envelope);
}
