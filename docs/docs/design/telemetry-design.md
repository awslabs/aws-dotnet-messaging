## Telemetry

It is expected a large numbers of messages will be going through a system using the AWS Message Processing Framework for .NET moving from publishers to subscribers.
Users of the library need access to telemetry that shows how messages traverse through their messaging system. For example, if an alarm goes off for a message that
failed to process, users need to be able to see where the message came from and how it got to the point it failed. Another need is total processing time for when a message
exceeds an SLA, the user needs to be able see where the bottlenecks are for messages being processed.

### Tenets

* Telemetry is an opt-in optional feature
* No performance cost if telemetry disabled
* Multiple telemetry providers supported with an extensible API to add more


## ITelemetryFactory

The core service interface used by the library is `ITelemetryFactory`. This library will be injected in critical points throughout the library. Users that want to add
their telemetry data can also inject `ITelemetryFactory` into their `IMessageHandler`.

`ITelemetryFactory` provides the abstraction on top of any registered provider to pass along the start telemetry trace calls into the registered providers. This allows the library to not care
which trace providers are registered or even if a trace provider is registered. The default implementation for this interface will look for all registered providers
and pass along the call to the provider.


```csharp
namespace AWS.Messaging.Telemetry;

/// <summary>
/// Service interface the AWS.Messaging library uses to start telemetry traces.
/// </summary>
public interface ITelemetryFactory
{
    /// <summary>
    /// Boolean indicating if telemetry is enabled.
    /// </summary>
    bool IsTelemetryEnabled { get; }

    /// <summary>
    /// Start a trace represented by the returned ITelemetryTrace. The trace will end when the ITelemetryTrace is disposed.
    /// </summary>
    /// <param name="traceName">The name of the trace.</param>
    /// <returns>The state of the trace.</returns>
    ITelemetryTrace Trace(string traceName);

    /// <summary>
    /// Start a trace represented by the returned ITelemetryTrace. The trace will end when the ITelemetryTrace is disposed.
    /// The MessageEnvelope is used to look for parent trace metadata to connect traces from publishers to subscribers.
    /// </summary>
    /// <param name="traceName"></param>
    /// <param name="envelope"></param>
    /// <returns></returns>
    ITelemetryTrace Trace(string traceName, MessageEnvelope envelope);
}
```


## ITelemetryTrace

The ITelemetryTrace is the library's abstraction for a trace. It allows the core library to add additional metadata to the trace like message id or message type.
This allows users to be able to search their telemetry system for traces by the metadata applied to the traces. When messages are going to leave the system
through a publisher the context of the trace can be recorded into the MessageEnvelope using the RecordTelemetryContext. This allows subscribers to
continue the trace by connected their telemetry traces with the telemetry context the telemetry provider stored in the MessageEnvelope's metadata.

When the `ITelemetryFactory` starts a trace it creates `ITelemetryTrace` from each of the registered telemetry providers. It will then create a composite
ITelemetryTrace maintaining a reference to all the individual ITelemetryTrace(s) from the telemetry providers. Each API call on the composite ITelemetryTrace will
be forwarded on to telemetry provider specific `ITelemetryTrace`.

The `ITelemetryTrace` extends from the IDisposable. To end a trace the `ITelemetryTrace` must be disposed.

```csharp
namespace AWS.Messaging.Telemetry;

/// <summary>
/// A telemetry trace where metadata and exceptions can be added. The trace is ended when this
/// instance is disposed.
/// </summary>
public interface ITelemetryTrace : IDisposable
{
    /// <summary>
    /// Add metadata to telemetry trace.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void AddMetadata(string key, object value);

    /// <summary>
    /// Add exception to telemetry trace.
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="fatal"></param>
    void AddException(Exception exception, bool fatal = true);

    /// <summary>
    /// Record the trace context in the MessageEnvelope metadata to support linking with downstream services.
    /// </summary>
    /// <param name="envelope"></param>
    void RecordTelemetryContext(MessageEnvelope envelope);
}
```

### Example start trace

The example below is what code in this library would look like for starting a trace. The MessageEnvelope is provided to start the trace to allow
the telemetry provider to look for metadata stored on the MessageEnvelope to connect the trace.

The AddMetadata API is used to attach all to the telemetry provider to attach additional metadata to its trace. If an exception is thrown the telemetry providers
are informed using the AddException API.

The trace is wrapped around a `using` block to ensure the trace is disposed when complete.

```csharp
public async Task<MessageProcessStatus> InvokeAsync(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping, CancellationToken token = default)
{
    using (var trace = _telemetryWriter.Trace("Processing message", messageEnvelope))
    {
        try
        {
            trace.AddMetadata(TelemetryKeys.MessageId, messageEnvelope.Id);
            trace.AddMetadata(TelemetryKeys.HandlerType, subscriberMapping.HandlerType.FullName!);

            var handler = _serviceProvider.GetService(subscriberMapping.HandlerType);

            // Additional error handler and invoke

        }
        catch (Exception e)
        {
            trace.AddException(e);
            throw;
        }
    }
}
```

## Telemetry Providers

The AWS Message Processing Framework for .NET provides support for [AWS X-Ray](https://aws.amazon.com/xray/) and [OpenTelemetry](https://opentelemetry.io/). Each provider is implemented
in separate packages ensuring users are only including the provider and its dependencies for the telemetry provider they want to use. Each provider package implements the
`ITelemetryProvider` interface from AWS.Messaging. Each package provide extension methods to the `IMessageBusBuilder` that adds their implementation of `ITelemetryProvider` to the dependency
container using IMessageBusBuilder's `AddAdditionalService` method.

Users and third-party vendors can add support for additional telemetry providers by implementing the `ITelemetryProvider` interface and providing extension methods to register
the interface.


```csharp
namespace AWS.Messaging.Telemetry;

/// <summary>
/// Interface for telemetry providers to implement. The implementation must be registered with the dependency injection container as a
/// service for ITelemetryProvider. The core library's ITelemetryFactory will forwarded trace starts to all registered ITelemetryProvider services.
/// </summary>
public interface ITelemetryProvider
{
    /// <summary>
    /// Start a trace represented by the returned ITelemetryTrace. The trace will end when the ITelemetryTrace is disposed.
    /// </summary>
    /// <param name="traceName"></param>
    /// <returns></returns>
    ITelemetryTrace Trace(string traceName);

    /// <summary>
    /// Start a trace represented by the returned ITelemetryTrace. The trace will end when the ITelemetryTrace is disposed.
    /// The MessageEnvelope is used to look for parent trace metadata to connect traces from publishers to subscribers.
    /// </summary>
    /// <param name="traceName"></param>
    /// <param name="envelope"></param>
    /// <returns></returns>
    ITelemetryTrace Trace(string traceName, MessageEnvelope envelope);
}
```

Each telemetry provider provides their own implementation of `ITelemetryTrace` that records the additional metadata provided by the library onto the provider's
specific trace construct. The provider specific `ITelemetryTrace` closes the trace when the `ITelemetryTrace` is disposed.

### AWS X-Ray

AWS X-Ray is an AWS service for creating and analyzing distributed traces. .NET support is provided through the **AWSXRayRecorder.Core** NuGet package. X-Ray uses the concept of
segments and subsegments to build up a trace. The `AWSXRayRecorder` singleton is the main object used which provides methods for creating and ending segments and subsegments
along with adding metadata.

When starting a trace the `AWSXRayRecorder.Instance.IsEntityPresent()` method is used to determine if a segment has been started. If false is returned then a segment must
be created using the `BeginSegment`. Otherwise a subsegment is created using the `BeginSubsegment`. The state of whether a segment or subsegment was created is maintained
in the X-Ray implementation of `ITelemetryTrace`. When the trace is being disposed that state is used to determine if `EndSegment` or `EndSubsegment` is called.

**TODO:** Research how segment data can be stored into MessageEnvelope metadata and segments can be linked from the stored metadata.

### OpenTelemetry

OpenTelemetry is an open standards telemetry framework that supports many vendors including AWS recording and analysing distributed traces.

In .NET OpenTelemetry is implemented through the System.Diagnostics `Activity` and `ActivitySource` types. The provider for OpenTelemetry maintains its singleton `ActivitySource`
where traces are started from using the `ActivitySource.StartActivity` method. If OpenTelemetry is not configured for capturing tracing data from this library the `ActivitySource`
will return back a **null** `Activity`.

The `Activity` is held onto by the OpenTelemtry implementation of `ITelemetryTrace` and used to record metadata added to traces. When the `ITelemetryTrace` is disposed the `Activity`
is disposed closing the trace.

#### Connecting traces
The `RecordTelemetryContext` from `ITelemetryTrace` will record the `Activity.ParentId` and `Activity.TraceStateString` properties as metadata on the `MessageEnvelope`
using the `otel.traceparent` and `otel.tracestate` keys.
```csharp
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
```

When creating a trace for a `MessageEnvelope` the OpenTelemetry ITelemetryProvider will look for the metadata saved from `RecordTelemetryContext` method. If the data
exist then a parent context is created and then add as an `ActivityLink` for the started `Activity`.
