// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.Lambda.Services;

/// <summary>
/// Factory for creating instances of <see cref="AWS.Messaging.Lambda.Services.ILambdaMessageProcessor" />. Users that want to use a custom <see cref="AWS.Messaging.Lambda.Services.ILambdaMessageProcessor" />
/// can inject into the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> their own implementation of <see cref="AWS.Messaging.Lambda.Services.ILambdaMessageProcessorFactory" /> to construct
/// a custom <see cref="AWS.Messaging.Lambda.Services.ILambdaMessageProcessor" /> instance.
/// </summary>
public interface ILambdaMessageProcessorFactory
{
    /// <summary>
    /// Create an instance of <see cref="AWS.Messaging.Lambda.Services.ILambdaMessageProcessor" /> for use in AWS Lambda functions.
    /// </summary>
    /// <returns></returns>
    ILambdaMessageProcessor CreateLambdaMessageProcessor(LambdaMessageProcessorConfiguration configuration);
}

/// <summary>
/// Implementation of <see cref="AWS.Messaging.Lambda.Services.ILambdaMessageProcessorFactory" /> that is the default registered factory into
/// the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> unless a user has registered their own implementation.
/// </summary>
internal class DefaultLambdaMessageProcessorFactory : ILambdaMessageProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc/>
    public DefaultLambdaMessageProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public ILambdaMessageProcessor CreateLambdaMessageProcessor(LambdaMessageProcessorConfiguration configuration)
    {
        return ActivatorUtilities.CreateInstance<DefaultLambdaMessageProcessor>(_serviceProvider, configuration);
    }
}
