// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SubscriberService.MessageHandlers;
using SubscriberService.Models;

var app = Host.CreateDefaultBuilder(args)
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
        services.AddAWSMessageBus(builder =>
        {
            // To load the configuration from appsettings.json instead of the code below, uncomment this and remove the following lines.
            // builder.LoadConfigurationFromSettings(context.Configuration);

            var mpfQueueUrl = "https://sqs.us-west-2.amazonaws.com/147997163238/MPF";
            if (string.IsNullOrEmpty(mpfQueueUrl))
                throw new InvalidOperationException("Missing required configuration parameter 'AWS:Resources:MPFQueueUrl'.");

            builder.AddSQSPoller(mpfQueueUrl);
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");

            builder.ConfigureSerializationOptions(options =>
            {
                options.SystemTextJsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
            });

            // Optional: Configure the backoff policy used by the SQS Poller.
            builder.ConfigureBackoffPolicy(options =>
            {
                options.UseCappedExponentialBackoff(x =>
                {
                    x.CapBackoffTime = 60;
                });
            });

            // Optional: Configure a PollingControlToken, you can call Start()/Stop() to start and stop message processing, by default it will be started
            builder.ConfigurePollingControlToken(new PollingControlToken
            {
                // Optional: Set how frequently it will check for changes to the state of the PollingControlToken
                PollingWaitTime = TimeSpan.FromMilliseconds(200)
            });

            // Logging data messages is disabled by default to protect sensitive user data. If you want this enabled, uncomment the line below.
            // builder.EnableMessageContentLogging();
        });
        services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                .AddSource(nameof(ChatMessageHandler))
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Subscriber"))
                .AddAWSInstrumentation()
                .AddXRayTraceId()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://52.12.96.156:4318/v1/traces");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                }));

    })
    .Build();

Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

await app.RunAsync();

