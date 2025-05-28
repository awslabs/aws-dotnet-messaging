// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Options;

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace AWS.Messaging.Telemetry.OpenTelemetry;

/// <summary>
/// Configuration options for <see cref="OpenTelemetryProvider"/>.
/// </summary>
public class OpenTelemetryProviderOptions
{
    /// <summary>
    /// Indicates whether the created spans should link to a parent activity, if available.
    /// When true, a Link is created instead of setting a parent directly.
    /// </summary>
    public bool ShouldLinkToParentActivity { get; set; } = true;
}

/// <summary>
/// Creates OpenTelemetry traces
/// </summary>
public class OpenTelemetryProvider : ITelemetryProvider
{
    private readonly Tracer _tracer;
    private readonly IOptions<OpenTelemetryProviderOptions> _options;

    /// <summary>
    /// Creates instance of <see cref="OpenTelemetryProvider"/>
    /// </summary>
    /// <param name="options">Instance of <see cref="IOptions{OpenTelemetryProviderOptions}"/></param>
    /// <param name="tracerProvider">Instance of <see cref="TracerProvider"/></param>
    public OpenTelemetryProvider(IOptions<OpenTelemetryProviderOptions> options, TracerProvider? tracerProvider = null)
    {
        _options = options;
        _tracer = (tracerProvider ?? TracerProvider.Default)
            .GetTracer(Constants.SourceName, TelemetryKeys.AWSMessagingAssemblyVersion);
    }

    /// <inheritdoc/>
	public ITelemetryTrace Trace(string traceName, ActivityKind activityKind = ActivityKind.Producer)
    {
        var span = _tracer.StartActiveSpan(
            traceName,
            MapToSpanKind(activityKind),
            Tracer.CurrentSpan);

        return new OpenTelemetryTrace(span);
    }

    /// <inheritdoc/>
    public ITelemetryTrace Trace(string traceName, MessageEnvelope envelope)
    {
        var propagationContext = Propagators.DefaultTextMapPropagator.Extract(
            default,
            envelope,
            ExtractTraceContextFromEnvelope);

        SpanContext spanContext = new(propagationContext.ActivityContext);

        TelemetrySpan span;

        if (_options.Value.ShouldLinkToParentActivity)
        {
            span = _tracer.StartActiveSpan(
                traceName,
                SpanKind.Consumer,
                Tracer.CurrentSpan,
                links: new Link[] { new(spanContext) });
        }
        else
        {
            Baggage.Current = propagationContext.Baggage;

            span = _tracer.StartActiveSpan(
                traceName,
                SpanKind.Consumer,
                spanContext);
        }

        return new OpenTelemetryTrace(span);
    }

    private static SpanKind MapToSpanKind(ActivityKind activityKind) => activityKind switch
    {
        ActivityKind.Consumer => SpanKind.Consumer,
        ActivityKind.Producer => SpanKind.Producer,
        ActivityKind.Server => SpanKind.Server,
        _ => throw new ArgumentOutOfRangeException(nameof(activityKind)),
    };

    /// <summary>
    /// Extracts propagation context from a <see cref="MessageEnvelope"/>, meant to be used with <see cref="TextMapPropagator"/>
    /// </summary>
    /// <param name="envelope">Inbound message envelope</param>
    /// <param name="key">Context key</param>
    /// <returns>Context value</returns>
    private static IEnumerable<string> ExtractTraceContextFromEnvelope(MessageEnvelope envelope, string key)
    {
        if (envelope.Metadata.TryGetValue(key, out var jsonElement))
        {
            return new string[] { jsonElement.ToString() };
        }

        return Enumerable.Empty<string>();
    }
}
