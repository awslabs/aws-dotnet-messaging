// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Telemetry;
using AWS.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.Telemetry;

/// <summary>
/// The default implementation of <see cref="ITelemetryFactory"/>.
/// </summary>
public class DefaultTelemetryFactory : ITelemetryFactory
{
    private const string TraceNamePrefix = "AWS.Messaging: ";
    readonly IList<ITelemetryProvider> _telemetryRecorders;

    /// <summary>
    /// Create the default instance of <see cref="DefaultTelemetryFactory"/>.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DefaultTelemetryFactory(IServiceProvider serviceProvider)
    {
        _telemetryRecorders = serviceProvider.GetServices<ITelemetryProvider>().ToList();
    }

    /// <inheritdoc/>
    public bool IsTelemetryEnabled => _telemetryRecorders.Any();

    /// <inheritdoc/>
    public ITelemetryTrace Trace(string traceName)
    {
        traceName = PrefixTraceName(traceName);

        var traces = new ITelemetryTrace[_telemetryRecorders.Count];
        for (var i = 0; i < _telemetryRecorders.Count; i++)
        {
            traces[i] = _telemetryRecorders[i].Trace(traceName);
        }

        return new CompositeTelemetryTrace(traces);
    }

    /// <inheritdoc/>
    public ITelemetryTrace Trace(string traceName, MessageEnvelope envelope)
    {
        traceName = PrefixTraceName(traceName);

        var traces = new ITelemetryTrace[_telemetryRecorders.Count];
        for (var i = 0; i < _telemetryRecorders.Count; i++)
        {
            traces[i] = _telemetryRecorders[i].Trace(traceName, envelope);
        }

        return new CompositeTelemetryTrace(traces);
    }

    private string PrefixTraceName(string traceName) => TraceNamePrefix + traceName;
}
