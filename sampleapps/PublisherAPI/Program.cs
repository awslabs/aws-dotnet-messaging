// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text.Json;
using AWS.Messaging.Telemetry.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Trace;
using PublisherAPI.Models;

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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAWSMessageBus(builder =>
{
    builder.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/626492997873/MPFTest", "ChatMessage");
    builder.AddSNSPublisher<OrderInfo>("arn:aws:sns:us-west-2:012345678910:MPF");
    builder.AddEventBridgePublisher<FoodItem>("arn:aws:events:us-west-2:012345678910:event-bus/default");
    builder.ConfigureSerializationOptions(options =>
    {
        options.SystemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy= JsonNamingPolicy.CamelCase,
        };
    });

    builder.AddOpenTelemetry();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
