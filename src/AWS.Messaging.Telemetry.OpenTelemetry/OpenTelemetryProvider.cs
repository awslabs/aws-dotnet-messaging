// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace AWS.Messaging.Telemetry.OpenTelemetry;

public class OpenTelemetryProvider : ITelemetryProvider
{
    internal const string ActivitySourceName = "AWS.Messaging";

    private static readonly ActivitySource AWSMessagingActivitySource = new ActivitySource(ActivitySourceName);

    public ITelemetryTrace StartTrace(string traceName)
    {
        Activity? parentToRestore = null;
        var activity = AWSMessagingActivitySource.StartActivity(traceName, ActivityKind.Server);
        if (activity == null)
        {
            // If we failed to create an activity then force the creation of a new root span
            // Use tips from this GitHub issue for creating a root: https://github.com/open-telemetry/opentelemetry-dotnet/issues/984
            parentToRestore = Activity.Current;
            Activity.Current = null;

            ActivityLink[]? links = null;
            if (parentToRestore != null)
            {
                links = new[] { new ActivityLink(parentToRestore!.Context) };
            }

            activity = AWSMessagingActivitySource.StartActivity(traceName, ActivityKind.Server, parentContext: default, links: links);
        }
        return new OpenTelemetryTrace(activity, parentToRestore);
    }

    /// <inheritdoc/>
    public ITelemetryTrace StartProcessMessageTrace(string traceName, MessageEnvelope envelope)
    {
        Activity? parentToRestore = Activity.Current;
        Activity.Current = null;

        ActivityContext parentContext = default;


        var textMapPropagator = Propagators.DefaultTextMapPropagator;
        if (textMapPropagator is not TraceContextPropagator)
        {
            var propagatedContext = textMapPropagator.Extract(default, envelope, HttpRequestHeaderValuesGetter);
            if(propagatedContext.ActivityContext.IsValid())
            {
                parentContext = propagatedContext.ActivityContext;
            }
        }


        ActivityLink[]? links = null;
        if (parentToRestore != null)
        {
            links = new[] { new ActivityLink(parentToRestore!.Context) };
        }

        var activity = AWSMessagingActivitySource.StartActivity(traceName, ActivityKind.Server, parentContext: parentContext, links: links);
        return new OpenTelemetryTrace(activity, parentToRestore);
    }

    private static readonly Func<MessageEnvelope, string, IEnumerable<string>> HttpRequestHeaderValuesGetter = (envelope, name) =>
        {
            if(envelope.Metadata.TryGetValue("otel." + name, out var value) && value is string svalue)
            {
                return new string[] { svalue };
            }

            return new string[0];
        };
}
