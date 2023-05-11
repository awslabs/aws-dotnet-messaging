// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using AWS.Messaging.Telemetry;
using System.Diagnostics;
using System;

namespace AWS.Messaging.Services;

/// <summary>
/// Service interface the AWS.Messaging library uses to write telemetry data.
/// </summary>
public interface ITelemetryWriter
{
    /// <summary>
    /// Boolean indicating if telemetry is enabled.
    /// </summary>
    bool IsTelemetryEnabled { get; }

    /// <summary>
    /// Start a trace till the return object is disposed.
    /// </summary>
    /// <param name="traceName">The name of the trace.</param>
    /// <returns>The state of the trace.</returns>
    ITelemetryTrace Trace(string traceName);

    /// <summary>
    /// Create a root trace for the start of processing a message.
    /// </summary>
    /// <param name="traceName"></param>
    /// <param name="envelope"></param>
    /// <returns></returns>
    ITelemetryTrace StartProcessMessageTrace(string traceName, MessageEnvelope envelope);
}

/// <summary>
/// The default implementation of <see cref="ITelemetryWriter"/>.
/// </summary>
public class DefaultTelemetryWriter : ITelemetryWriter
{
    private const string TraceNamePrefix = "AWS.Messaging: ";
    readonly IList<ITelemetryProvider> _telemetryRecorders;

    /// <summary>
    /// Create the default instance of <see cref="DefaultTelemetryWriter"/>.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DefaultTelemetryWriter(IServiceProvider serviceProvider)
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
            traces[i] = _telemetryRecorders[i].StartTrace(traceName);
        }

        return new CompositeTelemetryTrace(traces);
    }

    /// <inheritdoc/>
    public ITelemetryTrace StartProcessMessageTrace(string traceName, MessageEnvelope envelope)
    {
        traceName = PrefixTraceName(traceName);

        var traces = new ITelemetryTrace[_telemetryRecorders.Count];
        for (var i = 0; i < _telemetryRecorders.Count; i++)
        {
            traces[i] = _telemetryRecorders[i].StartProcessMessageTrace(traceName, envelope);
        }

        return new CompositeTelemetryTrace(traces);
    }

    private string PrefixTraceName(string traceName) => TraceNamePrefix + traceName;
}
