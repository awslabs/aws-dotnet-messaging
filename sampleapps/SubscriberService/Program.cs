// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
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
    .ConfigureServices((context, services) =>
    {

        var queueUrl = Environment.GetEnvironmentVariable("AWS_SQS_QUEUE_URL")
        ?? throw new InvalidOperationException("AWS_SQS_QUEUE_URL environment variable is required");

        // Register the AWS Message Processing Framework for .NET
        services.AddAWSMessageBus(builder =>
        {
            // Register an SQS Queue that the framework will poll for messages
            builder.AddSQSPoller(queueUrl);

            // Register all IMessageHandler implementations with the message type they should process. 
            // Here messages that match our ChatMessage .NET type will be handled by our ChatMessageHandler
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });
        // Configure OpenTelemetry with AWS X-Ray
        services.AddOpenTelemetryTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddXRayTraceId()
                .AddAWSInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SubscriberService"))
                .AddOtlpExporter(opts =>
                {
                    var endpoint = Environment.GetEnvironmentVariable("OTLP_ENDPOINT") 
                        ?? throw new InvalidOperationException("OTLP_ENDPOINT environment variable is required");
                    opts.Endpoint = new Uri(endpoint);
                });
        });

        // Set AWS X-Ray propagator for trace context
        OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

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
