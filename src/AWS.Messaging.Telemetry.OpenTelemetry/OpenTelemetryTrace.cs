// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.Trace;

namespace AWS.Messaging.Telemetry.OpenTelemetry;

internal class OpenTelemetryTrace : ITelemetryTrace
{
    private readonly Activity? _activity;
    private readonly Activity? _parentToRestore;

    public OpenTelemetryTrace(Activity? activity, Activity? parentToRestore = null)
    {
        _activity = activity;
        _parentToRestore = parentToRestore;
    }

    public void AddException(Exception exception, bool fatal = true)
    {
        _activity?.RecordException(exception);

        if(fatal)
        {
            _activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, exception.Message);
        }
    }

    public void AddMetadata(string key, object value)
    {
        _activity?.SetTag(key, value);
    }

    public void RecordTelemetryContext(MessageEnvelope envelope)
    {
        if (_activity == null)
            return;

        if(_activity.ParentId != null)
        {
            envelope.Metadata["otel.traceparent"] = _activity.ParentId;
        }

        if (!string.IsNullOrEmpty(_activity.TraceStateString))
            envelope.Metadata["otel.tracestate"] = _activity.TraceStateString;
    }


    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _activity?.Dispose();
            if(_parentToRestore != null)
            {
                Activity.Current = _parentToRestore;
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
