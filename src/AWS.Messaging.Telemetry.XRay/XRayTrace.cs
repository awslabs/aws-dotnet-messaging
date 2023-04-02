// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.XRay.Recorder.Core;

namespace AWS.Messaging.Telemetry.XRay;

internal class XRayTrace : ITelemetryTrace
{
    internal bool IsSubsegment { get; set; } = false;

    public void AddMetadata(string key, object value)
    {
        if (!AWSXRayRecorder.Instance.IsEntityPresent())
            return;

        AWSXRayRecorder.Instance.AddMetadata(key, value);
    }

    public void AddException(Exception exception, bool fatal = true)
    {
        if (!AWSXRayRecorder.Instance.IsEntityPresent())
            return;

        AWSXRayRecorder.Instance.AddException(exception);
    }

    private bool _disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (IsSubsegment)
                {
                    AWSXRayRecorder.Instance.EndSubsegment();
                }
                else
                {
                    AWSXRayRecorder.Instance.EndSegment();
                }
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
