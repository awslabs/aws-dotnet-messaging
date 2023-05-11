using Amazon.XRay.Recorder.Core;

namespace AWS.Messaging.Telemetry.XRay;

using AWS.Messaging.Telemetry;

public class AWSXRayTelemetryProvider : ITelemetryProvider
{
    public ITelemetryTrace StartTrace(string traceName)
    {
        var trace = new XRayTrace();
        if (AWSXRayRecorder.Instance.IsEntityPresent())
        {
            trace.IsSubsegment = true;
            AWSXRayRecorder.Instance.BeginSubsegment(traceName);
        }
        else
        {
            AWSXRayRecorder.Instance.BeginSegment(traceName);
        }

        return trace;
    }

    /// <inheritdoc/>
    public ITelemetryTrace StartProcessMessageTrace(string traceName, MessageEnvelope envelope)
    {
        // TODO: Grab parent segment information from envelope metadata

        var trace = new XRayTrace() { IsSubsegment = true };
        AWSXRayRecorder.Instance.BeginSegment(traceName);

        return trace;
    }
}
