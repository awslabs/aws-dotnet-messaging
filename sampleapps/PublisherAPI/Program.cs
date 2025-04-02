// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using AWS.Messaging.Telemetry.OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PublisherAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAWSMessageBus(bus =>
{
    // To load the configuration from appsettings.json instead of the code below, uncomment this and remove the following lines.
    // bus.LoadConfigurationFromSettings(builder.Configuration);

    // Standard SQS Queue
    var mpfQueueUrl = builder.Configuration["AWS:Resources:MPFQueueUrl"];
    bus.AddSQSPublisher<ChatMessage>(mpfQueueUrl, "chatMessage");

    // FIFO SQS Queue  
    var mpfFifoQueueUrl = builder.Configuration["AWS:Resources:MPFFIFOQueueUrl"];
    bus.AddSQSPublisher<TransactionInfo>(mpfFifoQueueUrl, "transactionInfo");

    // Standard SNS Topic
    var mpfTopicArn = builder.Configuration["AWS:Resources:MPFTopicArn"];
    bus.AddSNSPublisher<OrderInfo>(mpfTopicArn, "orderInfo");

    // FIFO SNS Topic
    var mpfFifoTopicArn = builder.Configuration["AWS:Resources:MPFFIFOTopicArn"];
    bus.AddSNSPublisher<BidInfo>(mpfFifoTopicArn, "bidInfo");

    // EventBridge Event Bus
    var mpfEventBusArn = builder.Configuration["AWS:Resources:MPFEventBusArn"];
    bus.AddEventBridgePublisher<FoodItem>(mpfEventBusArn, "foodItem");


    bus.ConfigureSerializationOptions(options =>
    {
        options.SystemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy= JsonNamingPolicy.CamelCase,
        };
    });

    // Logging data messages is disabled by default to protect sensitive user data. If you want this enabled, uncomment the line below.
    // bus.EnableMessageContentLogging();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("PublisherAPI"))
    .WithTracing(tracing => tracing
        .AddAWSMessagingInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
