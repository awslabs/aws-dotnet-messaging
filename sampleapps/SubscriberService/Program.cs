// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using SubscriberService.MessageHandlers;
using SubscriberService.Models;

var traceProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource("AWS.Messaging")
    .AddAWSInstrumentation()
//    .AddAspNetCoreInstrumentation()
    .AddConsoleExporter()
    .AddZipkinExporter(b =>
    {
        var zipkinHostName = Environment.GetEnvironmentVariable("ZIPKIN_HOSTNAME") ?? "localhost";
        b.Endpoint = new Uri($"http://localhost:9411/api/v2/spans");
    });
using var tracerProvider = traceProviderBuilder.Build();

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole().SetMinimumLevel(LogLevel.Debug);
    })
    .ConfigureServices(services =>
    {
        services.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/626492997873/MPFTest");
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("ChatMessage");
            builder.AddOpenTelemetry();
        });
    })
    .Build()
    .RunAsync();
