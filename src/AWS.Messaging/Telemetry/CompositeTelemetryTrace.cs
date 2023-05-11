// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Messaging.Telemetry;

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
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
