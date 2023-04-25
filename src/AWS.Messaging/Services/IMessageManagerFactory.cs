// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.Services;

/// <summary>
/// Factory for creating instances of <see cref="IMessageManager" />. Users that want to use a custom <see cref="IMessageManager" />
/// can inject into the <see cref="IServiceCollection" /> their own implementation of <see cref="IMessageManagerFactory" /> to construct
/// a custom <see cref="IMessageManager" /> instance.
/// </summary>
public interface IMessageManagerFactory
{
    /// <summary>
    /// Create an instance of <see cref="IMessageManager" />
    /// </summary>
    /// <param name="pollerConfiguration">This configuration controls the polling process and lifecycle of SQS messages</param>
    /// <returns></returns>
    IMessageManager CreateMessageManager(IMessagePollerConfiguration pollerConfiguration);
}

/// <summary>
/// Implementation of <see cref="AWS.Messaging.Services.IMessageManagerFactory" /> that is the default registered factory into
/// the IServiceCollection unless a user has registered their own implementation.
/// </summary>
internal class DefaultMessageManagerFactory : IMessageManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc/>
    public DefaultMessageManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public IMessageManager CreateMessageManager(IMessagePollerConfiguration pollerConfiguration)
    {
        IMessageManager messageManager;
        if (pollerConfiguration is SQSMessagePollerConfiguration sqsPollerConfiguration)
        {
            messageManager = ActivatorUtilities.CreateInstance<DefaultMessageManager>(_serviceProvider, sqsPollerConfiguration);
        }
        else
        {
            throw new ConfigurationException($"Invalid poller configuration type: {pollerConfiguration.GetType().FullName}");
        }

        return messageManager;
    }
}
