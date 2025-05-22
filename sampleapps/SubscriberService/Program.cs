// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Amazon.DynamoDBv2;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
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
        // Configure OpenTelemetry with AWS X-Ray
        services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddXRayTraceId()
                .AddAWSInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SubscriberService"))
                .AddOtlpExporter();
        });

        // Set AWS X-Ray propagator for trace context
        OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

        // Add AWS DynamoDB client
        services.AddAWSService<IAmazonDynamoDB>();

        // Configure AWS Message Bus using configuration
        services.AddAWSMessageBus(builder =>
        {
            builder.LoadConfigurationFromSettings(context.Configuration);

            builder.ConfigureSerializationOptions(options =>
            {
                options.SystemTextJsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
            });
        });
    })
    .Build()
    .RunAsync();
