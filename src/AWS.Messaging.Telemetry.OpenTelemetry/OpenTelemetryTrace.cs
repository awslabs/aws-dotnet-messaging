// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text.Json;
using AWS.Messaging.Internal;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace AWS.Messaging.Telemetry.OpenTelemetry;

/// <summary>
/// An OpenTelemetry trace (wrapper around a <see cref="Activity"/>)
/// </summary>
public class OpenTelemetryTrace : ITelemetryTrace
{
    private bool _disposedValue;
    private readonly TelemetrySpan _span;

    /// <summary>
    ///
    /// </summary>
    /// <param name="span"></param>
    public OpenTelemetryTrace(TelemetrySpan span)
    {
        _span = span;
    }

    /// <inheritdoc/>
    public void AddException(Exception exception, bool fatal = true)
    {
        _ = _span.RecordException(exception);

        if (fatal)
        {
            _span.SetStatus(Status.Error.WithDescription(exception.Message));
        }
    }

    /// <inheritdoc/>
    public void AddMetadata(string key, object value)
        => _span.SetAttribute(key, value?.ToString());

    /// <inheritdoc/>
    public void RecordTelemetryContext(MessageEnvelope envelope)
    {
        PropagationContext propagationContext = new(_span.Context, Baggage.Current);

        Propagators.DefaultTextMapPropagator
            .Inject(propagationContext, envelope, InjectTraceContextIntoEnvelope);
    }

    /// <summary>
    /// Stores propagation context in the <see cref="MessageEnvelope"/>, meant to be used with <see cref="TextMapPropagator"/>
    /// </summary>
    /// <param name="envelope">Outbound message envelope</param>
    /// <param name="key">Context key</param>
    /// <param name="value">Context value</param>
    private static void InjectTraceContextIntoEnvelope(MessageEnvelope envelope, string key, string value)
    {
        envelope.Metadata[key] = JsonSerializer.SerializeToElement(value, typeof(string), MessagingJsonSerializerContext.Default);
    }

    /// <summary>
    /// Releases resources related to the OpenTelemetry span.
    /// Ends the span if called from Dispose.
    /// </summary>
    /// <param name="disposing">
    /// true if called from Dispose; false if called from a finalizer.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _span.End();
            }

            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }
}
