// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.Telemetry;
using AWS.Messaging.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessageBusBuilderExtensions
{
    public static IMessageBusBuilder AddOpenTelemetry(this IMessageBusBuilder builder)
    {
        var serviceDescriptor = new ServiceDescriptor(typeof(ITelemetryProvider), typeof(OpenTelemetryProvider), ServiceLifetime.Singleton);
        builder.AddAdditionalService(serviceDescriptor);
        return builder;
    }
}
