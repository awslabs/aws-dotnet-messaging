// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.SQS;

/// <summary>
/// SQS implementation of the <see cref="AWS.Messaging.Services.IMessagePoller" />
/// </summary>
internal class SQSMessagePoller : IMessagePoller
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SQSMessagePoller> _logger;
    private readonly IMessageManager _messageManager;
    private readonly SQSMessagePollerConfiguration _configuration;

    /// <summary>
    /// Creates instance of <see cref="AWS.Messaging.SQS.SQSMessagePoller" />
    /// </summary>
    /// <param name="serviceProvider"><see cref="System.IServiceProvider" /> container used for acquiring and creating dependent services.</param>
    /// <param name="logger">Logger for debugging information.</param>
    /// <param name="messageManagerFactory">The factory to create the message manager for processing messages.</param>
    /// <param name="sqsClient">SQS service client to use for service API calls.</param>
    /// <param name="configuration">The SQS message poller configuration.</param>
    public SQSMessagePoller(IServiceProvider serviceProvider, ILogger<SQSMessagePoller> logger, IMessageManagerFactory messageManagerFactory, IAmazonSQS sqsClient, SQSMessagePollerConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sqsClient = sqsClient;
        _configuration = configuration;

        _messageManager = messageManagerFactory.CreateMessageManager(this);
    }

    /// <inheritdoc/>
    public Task StartPollingAsync(CancellationToken token = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ExtendMessageVisiblityTimeoutAsync(IEnumerable<MessageEnvelope> messages) => throw new NotImplementedException();

}
