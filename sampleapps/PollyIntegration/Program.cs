using AWS.Messaging.Services.Backoff;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AWS.Messaging.Telemetry.OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;
using PollyIntegration;
using PollyIntegration.MessageHandlers;
using PollyIntegration.Models;

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole().SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureAppConfiguration(configuration =>
    {
        configuration.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((context, services) =>
    {
        services.AddResiliencePipeline("my-pipeline", builder =>
        {
            builder
                .AddRetry(new RetryStrategyOptions())
                .AddTimeout(TimeSpan.FromSeconds(10));
        });

        services.TryAddSingleton<IBackoffHandler, PollyBackoffHandler>();

        services.AddAWSMessageBus(builder =>
            {
                // To load the configuration from appsettings.json instead of the code below, uncomment this and remove the following lines.
                // builder.LoadConfigurationFromSettings(context.Configuration);

                builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MPF");
                builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");

                // Logging data messages is disabled by default to protect sensitive user data. If you want this enabled, uncomment the line below.
                // builder.EnableMessageContentLogging();
            })
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("PollyIntegration"))
            .WithTracing(tracing => tracing
                .AddAWSMessagingInstrumentation()
                .AddConsoleExporter());
    })
    .Build()
    .RunAsync();
