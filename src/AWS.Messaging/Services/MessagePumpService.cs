// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Services;

/// <summary>
/// Starts an instance of an <see cref="IMessagePoller" /> for each
/// configured <see cref="IMessagePollerConfiguration" />
/// </summary>
internal class MessagePumpService : BackgroundService
{
    private readonly ILogger<MessagePumpService> _logger;
    private readonly IMessageConfiguration _messageConfiguration;
    private readonly IMessagePollerFactory _messagePollerFactory;

    /// <summary>
    /// Creates an instance of <see cref="MessagePumpService" />
    /// </summary>
    /// <param name="logger">Logger for debugging information</param>
    /// <param name="messageConfiguration">Configuration containing one or more <see cref="IMessagePollerConfiguration"/> instances to poll</param>
    /// <param name="messagePollerFactory">Factory for creating a <see cref="IMessagePoller"/> for each configuration</param>
    public MessagePumpService(ILogger<MessagePumpService> logger, IMessageConfiguration messageConfiguration, IMessagePollerFactory messagePollerFactory)
    {
        _logger = logger;
        _messageConfiguration = messageConfiguration;
        _messagePollerFactory = messagePollerFactory;
    }

    /// <summary>
    /// Starts an instance of an <see cref="IMessagePoller" /> for each
    /// configured <see cref="IMessagePollerConfiguration" />
    /// </summary>
    /// <param name="stoppingToken">Cancellation token that is passed into each poller</param>
    /// <returns>A task representing all the pollers that were started</returns>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        if (_messageConfiguration.MessagePollerConfigurations.Any())
        {
            foreach (var pollerConfiguration in _messageConfiguration.MessagePollerConfigurations)
            {
                var messagePoller = _messagePollerFactory.CreateMessagePoller(pollerConfiguration);

                _logger.LogInformation("Starting polling: {SubscriberEndpoint}", pollerConfiguration.SubscriberEndpoint);

                var task = messagePoller.StartPollingAsync(stoppingToken);
                task.ContinueWith(completedPollerTask =>
                {
                    if (completedPollerTask.IsFaulted && !stoppingToken.IsCancellationRequested )
                    {
                        _logger.LogError(completedPollerTask.Exception, "Poller for {SubscriberEndpoint} failed for an unexpected reason.", pollerConfiguration.SubscriberEndpoint);
                    }
                });
                tasks.Add(task);
            }
        }

        return Task.WhenAll(tasks);
    }
}
