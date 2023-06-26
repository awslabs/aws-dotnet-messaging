// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
namespace AWS.Messaging.Telemetry;

/// <summary>
/// Interface for telemetry providers to implement. The implementation must be registered with the dependency injection container as a
/// service for <see cref="ITelemetryProvider"/>. The core library's <see cref="ITelemetryFactory"/> will forwarded trace starts to all registered <see cref="ITelemetryProvider"/> services.
/// </summary>
public interface ITelemetryProvider
{
    /// <summary>
    /// Start a trace represented by the returned <see cref="ITelemetryTrace"/>. The trace will end when the <see cref="ITelemetryTrace"/> is disposed.
    /// </summary>
    /// <param name="traceName"></param>
    /// <returns></returns>
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
