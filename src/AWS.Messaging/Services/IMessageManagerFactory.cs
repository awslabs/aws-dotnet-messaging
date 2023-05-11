// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.Services;

/// <summary>
/// Factory for creating instances of <see cref="AWS.Messaging.Services.IMessageManager" />. Users that want to use a custom <see cref="AWS.Messaging.Services.IMessageManager" />
/// can inject into the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> their own implementation of <see cref="AWS.Messaging.Services.IMessageManagerFactory" /> to construct
/// a custom <see cref="AWS.Messaging.Services.IMessageManager" /> instance.
/// </summary>
public interface IMessageManagerFactory
{
    /// <summary>
    /// Create an instance of <see cref="AWS.Messaging.Services.IMessageManager" />
    /// </summary>
    /// <param name="poller">The <see cref="AWS.Messaging.Services.IMessagePoller" /> that the <see cref="AWS.Messaging.Services.IMessageManager" /> to make lifecycle changes to the message.</param>
    /// <param name="configuration">The configuration for the message manager.</param>
    /// <returns>New instance of an <see cref="IMessageManager"/></returns>
    internal IMessageManager CreateMessageManager(IMessagePoller poller, MessageManagerConfiguration configuration);
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
    public IMessageManager CreateMessageManager(IMessagePoller poller, MessageManagerConfiguration configuration)
    {
        return ActivatorUtilities.CreateInstance<DefaultMessageManager>(_serviceProvider, poller, configuration);
    }
}
