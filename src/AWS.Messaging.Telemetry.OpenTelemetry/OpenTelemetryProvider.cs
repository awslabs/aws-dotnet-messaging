// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace AWS.Messaging.Telemetry.OpenTelemetry;

public class OpenTelemetryProvider : ITelemetryProvider
{
    internal const string ActivitySourceName = "AWS.Messaging";

    private static readonly ActivitySource AWSMessagingActivitySource = new ActivitySource(ActivitySourceName);

    public ITelemetryTrace StartTrace(string traceName)
    {
        var activity = AWSMessagingActivitySource.StartActivity(traceName, ActivityKind.Client);
        return new OpenTelemetryTrace(activity);
    }
}
