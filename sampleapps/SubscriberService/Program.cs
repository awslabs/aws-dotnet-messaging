// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SQS;
using AWS.Messaging.Telemetry.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
        AWSConfigs.LoggingConfig.LogMetricsFormat = LogMetricsFormatOption.JSON;
        AWSConfigs.LoggingConfig.LogResponses = ResponseLoggingOption.Always;
        AWSConfigs.LoggingConfig.LogMetrics = true;

        var sqsClient = new AmazonSQSClient(new AmazonSQSConfig
        {
            MaxErrorRetry = 0
        });
        services.TryAddSingleton<IAmazonSQS>(sqsClient);
        services.AddAWSMessageBus(builder =>
        {
            // To load the configuration from appsettings.json instead of the code below, uncomment this and remove the following lines.
            // builder.LoadConfigurationFromSettings(context.Configuration);

            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MPF");
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");

            // Optional: Configure the backoff policy used by the SQS Poller.
            builder.ConfigureBackoffPolicy(options =>
            {
                options.UseCappedExponentialBackoff(x =>
                {
                    x.CapBackoffTime = 3600000;
                });
            });

            // Logging data messages is disabled by default to protect sensitive user data. If you want this enabled, uncomment the line below.
            // builder.EnableMessageContentLogging();
        });
    })
    .Build()
    .RunAsync();
