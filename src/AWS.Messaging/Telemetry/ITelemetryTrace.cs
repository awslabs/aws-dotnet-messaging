// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
namespace AWS.Messaging.Telemetry;

/// <summary>
/// A telemetry trace where metadata and exceptions can be added. The trace is ended with this
/// instance is disposed.
/// </summary>
public interface ITelemetryTrace : IDisposable
{
    /// <summary>
    /// Add metadata to telemetry trace.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void AddMetadata(string key, object value);

    /// <summary>
    /// Add exception to telemetry trace.
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="fatal"></param>
    void AddException(Exception exception, bool fatal = true);

    /// <summary>
    /// Record in the MessageEnvelope metadata the trace context for downstream services to link with
    /// </summary>
    /// <param name="envelope"></param>
    void RecordTelemetryContext(MessageEnvelope envelope);
}
