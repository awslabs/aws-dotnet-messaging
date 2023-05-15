// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.SQS;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.Services;

/// <summary>
/// Factory for creating instances of <see cref="AWS.Messaging.Services.IMessagePoller" />. Users that want to use a custom <see cref="AWS.Messaging.Services.IMessagePoller" />
/// can inject into the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> their own implementation of <see cref="AWS.Messaging.Services.IMessagePollerFactory" /> to construct
/// a custom <see cref="AWS.Messaging.Services.IMessagePoller" /> instance.
/// </summary>
public interface IMessagePollerFactory
{
    /// <summary>
    /// Create an instance of <see cref="AWS.Messaging.Services.IMessagePoller" /> for the given resource type.
    /// </summary>
    /// <param name="pollerConfiguration">The configuration for the poller.</param>
    /// <returns></returns>
    IMessagePoller CreateMessagePoller(IMessagePollerConfiguration pollerConfiguration);
}

/// <summary>
/// Implementation of <see cref="AWS.Messaging.Services.IMessagePollerFactory" /> that is the default registered factory into
/// the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> unless a user has registered their own implementation.
/// </summary>
internal class DefaultMessagePollerFactory : IMessagePollerFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc/>
    public DefaultMessagePollerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public IMessagePoller CreateMessagePoller(IMessagePollerConfiguration pollerConfiguration)
    {
        IMessagePoller poller;
        if(pollerConfiguration is SQSMessagePollerConfiguration sqsPollerConfiguration)
        {
            poller = ActivatorUtilities.CreateInstance<SQSMessagePoller>(_serviceProvider, sqsPollerConfiguration);
        }
        else
        {
            throw new ConfigurationException($"Invalid poller configuration type: {pollerConfiguration.GetType().FullName}");
        }

        return poller;
    }
}
