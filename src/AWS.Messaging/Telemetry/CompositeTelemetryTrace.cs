// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0
namespace AWS.Messaging.Telemetry;

/// <summary>
/// Wraps all of the telemetry provider specific <see cref="ITelemetryTrace"/> and forwards each tracing API to all
/// of the created provider specific ITelemetryTrace instances.
/// </summary>
internal class CompositeTelemetryTrace : ITelemetryTrace
{
    private readonly ITelemetryTrace[] _traces;

    public CompositeTelemetryTrace(ITelemetryTrace[] traces)
    {
        _traces = traces;
    }

    /// <inheritdoc/>
    public void AddMetadata(string key, object value)
    {
        foreach (var trace in _traces)
        {
            trace.AddMetadata(key, value);
        }
    }

    /// <inheritdoc/>
    public void AddException(Exception exception, bool fatal = true)
    {
        foreach (var trace in _traces)
        {
            trace.AddException(exception, fatal);
        }
    }

    public void RecordTelemetryContext(MessageEnvelope envelope)
    {
        foreach (var trace in _traces)
        {
            trace.RecordTelemetryContext(envelope);
        }
    }

    private bool _disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            foreach (var trace in _traces)
            {
                trace.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
