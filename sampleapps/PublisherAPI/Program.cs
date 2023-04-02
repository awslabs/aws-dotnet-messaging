// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Text.Json;
using AWS.Messaging.Telemetry.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Trace;
using PublisherAPI.Models;

var provider = new OpenTelemetryProvider();

ActivitySource MyActivitySource = new(
    "MyCompany.MyProduct.MyLibrary");

var traceProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .AddSource("AWS.Messaging")
    .AddSource("MyCompany.MyProduct.MyLibrary")
    .AddConsoleExporter();
using var tracerProvider = traceProviderBuilder.Build();


using (var activity = MyActivitySource.StartActivity("SayHello"))
{
    activity?.SetTag("foo", 1);
    activity?.SetTag("bar", "Hello, World!");
    activity?.SetTag("baz", new int[] { 1, 2, 3 });
    activity?.SetStatus(ActivityStatusCode.Ok);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAWSMessageBus(builder =>
{
    builder.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/626492997873/MPFTest");
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
