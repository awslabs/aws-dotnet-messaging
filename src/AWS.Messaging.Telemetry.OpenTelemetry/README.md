# OpenTelemetry plugin for AWS Message Processing Framework for .NET
[![nuget](https://img.shields.io/nuget/v/AWS.Messaging.Telemetry.OpenTelemetry.svg) ![downloads](https://img.shields.io/nuget/dt/AWS.Messaging.Telemetry.OpenTelemetry.svg)](https://www.nuget.org/packages/AWS.Messaging.Telemetry.OpenTelemetry/)

**Notice:** *This library is still in early active development and is not ready for use beyond experimentation.*

This package is an [Instrumentation
Library](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/glossary.md#instrumentation-library), which instruments the [AWS Message Processing Framework for .NET](https://github.com/awslabs/aws-dotnet-messaging) to collect traces about 
messages that are sent and received.

## Configuration

### 1. Install Packages

Add a reference to [`AWS.Messaging.Telemetry.OpenTelemetry`](https://www.nuget.org/packages/AWS.Messaging.Telemetry.OpenTelemetry). In this example, we're going to configure OpenTelemetry on our `IServiceCollection`, so also add a reference to [`OpenTelemetry.Extensions.Hosting`](https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting). This is not required if starting and stopping tracing via `CreateTracerProviderBuilder`. 

You may also add a reference to one or more [exporters](https://opentelemetry.io/docs/instrumentation/net/exporters/) to visualize your telemetry data.

```shell
dotnet add package AWS.Messaging.Telemetry.OpenTelemetry --prerelease
dotnet add package OpenTelemetry.Extensions.Hosting
```

### 2. Enable Instrumentation
In the `Startup` class add a call to `AddOpenTelemetry` to configure OpenTelemetry. On the `TracerProviderBuilder`, call `AddAWSMessagingInstrumentation` to begin capturing traces for the AWS Message Processing Framework for .NET.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MPF");
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");
        });

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("myApplication"))
            .WithTracing(tracing => tracing
                .AddAWSMessagingInstrumentation()
                .AddConsoleExporter());
    }
}
```

# Useful Links
* [AWS Message Processing Framework for .NET Design Document](../../docs/design/message-processing-framework-design.md)

# Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

# License

This project is licensed under the Apache-2.0 License.
