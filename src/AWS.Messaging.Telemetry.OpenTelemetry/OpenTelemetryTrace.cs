// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.Trace;

namespace AWS.Messaging.Telemetry.OpenTelemetry;

internal class OpenTelemetryTrace : ITelemetryTrace
{
    private readonly Activity? _activity;

    public OpenTelemetryTrace(Activity? activity)
    {
        _activity = activity;
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


    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _activity?.Dispose();
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
