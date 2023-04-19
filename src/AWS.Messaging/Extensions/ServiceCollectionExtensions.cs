// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds extension methods to <see cref="IServiceCollection"/> to allow the configuration of the framework at application startup.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IMessageBusBuilder"/> to the dependency injection framework 
    /// to allow access to the framework components throughout the application.
    /// Allows the configuration of the messaging framework by exposing methods to add publishers and subscribers.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="builder">An action to define the message framework configuration using <see cref="MessageBusBuilder"/></param>
    public static IServiceCollection AddAWSMessageBus(this IServiceCollection services, Action<MessageBusBuilder> builder)
    {
        var messageBusBuilder = new MessageBusBuilder();

        builder(messageBusBuilder);
        messageBusBuilder.Build(services);

        return services;
    }
}
