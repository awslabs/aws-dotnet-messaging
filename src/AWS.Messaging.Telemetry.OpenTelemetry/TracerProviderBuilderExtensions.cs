// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace AWS.Messaging.Telemetry.OpenTelemetry;

/// <summary>
/// Extensions for a <see cref="TracerProviderBuilder"/> to enable instrumentation for AWS Messaging
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Enables AWS Messaging Instrumentation for OpenTelemetry
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
    public static TracerProviderBuilder AddAWSMessagingInstrumentation(this TracerProviderBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ITelemetryProvider, OpenTelemetryProvider>();
        });

        builder.AddSource(Constants.SourceName);

        return builder;
    }
}
