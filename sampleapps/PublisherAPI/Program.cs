// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PublisherAPI.Models;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure OpenTelemetry with AWS X-Ray
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        .AddAspNetCoreInstrumentation()
        .AddXRayTraceId()
        .AddAWSInstrumentation()
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("PublisherAPI"))
        .AddOtlpExporter();
});

// Set AWS X-Ray propagator for trace context
OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

// Configure AWS Message Bus with single SQS publisher
builder.Services.AddAWSMessageBus(bus =>
{
    // Load configuration from appsettings.json
    bus.LoadConfigurationFromSettings(builder.Configuration);

    bus.ConfigureSerializationOptions(options =>
    {
        options.SystemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    });
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
