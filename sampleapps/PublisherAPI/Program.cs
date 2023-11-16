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

    bus.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/012345678910/MPF", "chatMessage");
    bus.AddSNSPublisher<OrderInfo>("arn:aws:sns:us-west-2:012345678910:MPF", "orderInfo");
    bus.AddEventBridgePublisher<FoodItem>("arn:aws:events:us-west-2:012345678910:event-bus/default", "foodItem");

    // FIFO endpoints
    bus.AddSQSPublisher<TransactionInfo>("https://sqs.us-west-2.amazonaws.com/012345678910/MPF.fifo", "transactionInfo");
    bus.AddSNSPublisher<BidInfo>("arn:aws:sns:us-west-2:012345678910:MPF.fifo", "bidInfo");

    bus.ConfigureSerializationOptions(options =>
    {
        options.SystemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy= JsonNamingPolicy.CamelCase,
        };
    });
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
