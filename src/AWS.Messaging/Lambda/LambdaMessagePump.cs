// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Lambda;

/// <summary>
/// This is an internal implementation of <see cref="ILambdaMessagePump"/>
/// </summary>
internal class LambdaMessagePump : ILambdaMessagePump
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<MessagePumpService> _logger;
    private readonly IMessageConfiguration _messageConfiguration;
    private readonly IMessagePollerFactory _messagePollerFactory;

    /// <summary>
    /// Creates an instance of <see cref="LambdaMessagePump" />
    /// </summary>
    /// <param name="clientProvider">Provides the AWS service client from the DI container</param>
    /// <param name="logger">Logger for debugging information</param>
    /// <param name="messageConfiguration">Configuration containing one or more <see cref="IMessagePollerConfiguration"/> instances to poll</param>
    /// <param name="messagePollerFactory">Factory for creating a <see cref="IMessagePoller"/> for each configuration</param>
    public LambdaMessagePump(IAWSClientProvider clientProvider, ILogger<MessagePumpService> logger, IMessageConfiguration messageConfiguration, IMessagePollerFactory messagePollerFactory)
    {
        _sqsClient = clientProvider.GetServiceClient<IAmazonSQS>();
        _logger = logger;
        _messageConfiguration = messageConfiguration;
        _messagePollerFactory = messagePollerFactory;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(SQSEvent sqsEvent, CancellationToken stoppingToken)
    {
        if (!sqsEvent.Records.Any())
            return;

        var sqsQueueArn = sqsEvent.Records[0].EventSourceArn;
        var sqsQueueUrl = await GetSQSQueueUrl(sqsQueueArn);

        var pollerConfiguration = _messageConfiguration.GetLambdaMessagePollerConfiguration(sqsQueueUrl);
        if (pollerConfiguration is null)
        {
            _logger.LogError("Could not find any lambdaMessagePollerConfiguration with {queueUrl} as the subscriber endpoint", sqsQueueUrl);
            return;
        }

        var lambdaMessagePollerConfiguration = (LambdaMessagePollerConfiguration)pollerConfiguration;
        lambdaMessagePollerConfiguration.SQSEvent = sqsEvent;
        var lambdaMessagePoller = _messagePollerFactory.CreateMessagePoller(lambdaMessagePollerConfiguration);
        var task = lambdaMessagePoller.StartPollingAsync(stoppingToken);

        await task.ContinueWith(completedPollerTask =>
        {
            if (completedPollerTask.IsFaulted && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(completedPollerTask.Exception, "Lambda message poller for {SubscriberEndpoint} failed for an unexpected reason.", pollerConfiguration.SubscriberEndpoint);
            }
        });
    }

    /// <inheritdoc/>
    public async Task<SQSBatchResponse> ExecuteWithSQSBatchResponseAsync(SQSEvent sqsEvent, CancellationToken stoppingToken)
    {
        if (!sqsEvent.Records.Any())
            return new SQSBatchResponse();

        var sqsQueueArn = sqsEvent.Records[0].EventSourceArn;
        var sqsQueueUrl = await GetSQSQueueUrl(sqsQueueArn);

        var pollerConfiguration = _messageConfiguration.GetLambdaMessagePollerConfiguration(sqsQueueUrl);
        if (pollerConfiguration is null)
        {
            _logger.LogError("Could not find any lambdaMessagePollerConfiguration with {queueUrl} as the subscriber endpoint", sqsQueueUrl);
            return new SQSBatchResponse();
        }

        // Mark all messages as failed initially. Messages that are successfully process will be removed from the list.
        var sqsBatchResponse = new SQSBatchResponse();
        foreach (var message in sqsEvent.Records)
        {
            var batchItemFailure = new SQSBatchResponse.BatchItemFailure();
            batchItemFailure.ItemIdentifier = message.MessageId;
            sqsBatchResponse.BatchItemFailures.Add(batchItemFailure);
        }

        var lambdaMessagePollerConfiguration = (LambdaMessagePollerConfiguration)pollerConfiguration;
        lambdaMessagePollerConfiguration.SQSEvent = sqsEvent;
        lambdaMessagePollerConfiguration.SQSBatchResponse = sqsBatchResponse;
        var lambdaMessagePoller = _messagePollerFactory.CreateMessagePoller(lambdaMessagePollerConfiguration);
        var task = lambdaMessagePoller.StartPollingAsync(stoppingToken);

        await task.ContinueWith(completedPollerTask =>
        {
            if (completedPollerTask.IsFaulted && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(completedPollerTask.Exception, "Lambda message poller for {SubscriberEndpoint} failed for an unexpected reason.", pollerConfiguration.SubscriberEndpoint);
            }
        });

        return sqsBatchResponse;
    }

    private async Task<string> GetSQSQueueUrl(string queueArn)
    {
        // Example ARN - arn:aws:sqs:us-west-2:888888888888:LambdaSQSDemo
        // The last segment is the queue name
        var arnSegments = queueArn.Split(':');
        var queueName = arnSegments[arnSegments.Length - 1];
        var response = await _sqsClient.GetQueueUrlAsync(queueName);
        return response.QueueUrl;
    }
}
