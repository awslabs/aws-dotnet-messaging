using Amazon.Lambda.Annotations;
using AWS.Messaging.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LambdaMessaging;

[LambdaStartup]
public class Startup
{
    public HostApplicationBuilder ConfigureHostBuilder()
    {
        var builder = new HostApplicationBuilder();
        builder.Services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddLambdaLogger();
        });
        builder.Services.AddAWSMessageBus(b =>
        {
            b.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");

            b.AddLambdaMessageProcessor(options =>
            {
                options.MaxNumberOfConcurrentMessages = 2;
            });
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("LambdaService"))
            .WithTracing(tracing => tracing
                .AddAWSMessagingInstrumentation()
                .AddXRayTraceId()
                .AddOtlpExporter());

        return builder;
    }
}
