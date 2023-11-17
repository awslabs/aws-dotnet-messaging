// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using AWS.Messaging.Telemetry.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SubscriberService.MessageHandlers;
using SubscriberService.Models;

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
        services.AddAWSMessageBus(builder =>
        {
            // To load the configuration from appsettings.json instead of the code below, uncomment this and remove the following lines.
            // builder.LoadConfigurationFromSettings(context.Configuration);

            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MPF");
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");

            builder.ConfigureSerializationOptions(options =>
            {
                options.SystemTextJsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
            });
        })
        .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("SubscriberService"))
            .WithTracing(tracing => tracing
                .AddAWSMessagingInstrumentation()
                .AddConsoleExporter());
    })
    .Build()
    .RunAsync();
