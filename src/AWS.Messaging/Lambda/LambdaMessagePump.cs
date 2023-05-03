// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.SQSEvents;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Lambda;

/// <summary>
/// This is an internal implementation of <see cref="ILambdaMessagePump"/>
/// </summary>
internal class LambdaMessagePump : ILambdaMessagePump
{
    private readonly ILogger<MessagePumpService> _logger;
    private readonly IMessagePollerFactory _messagePollerFactory;

    /// <summary>
    /// Creates an instance of <see cref="LambdaMessagePump" />
    /// </summary>
    /// <param name="logger">Logger for debugging information</param>
    /// <param name="messagePollerFactory">Factory for creating a <see cref="IMessagePoller"/> for each configuration</param>
    public LambdaMessagePump(ILogger<MessagePumpService> logger, IMessagePollerFactory messagePollerFactory)
    {
        _logger = logger;
        _messagePollerFactory = messagePollerFactory;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(SQSEvent sqsEvent, CancellationToken stoppingToken, LambdaMessagePollerOptions? options = null)
    {
        if (!sqsEvent.Records.Any())
            return;

        var lambdaMessagePollerConfiguration = CreatelambdaMessagePollerConfiguration(sqsEvent, isPartialBatchResponseEnabled: false, options);
        var lambdaMessagePoller = _messagePollerFactory.CreateMessagePoller(lambdaMessagePollerConfiguration);
        await lambdaMessagePoller.StartPollingAsync(stoppingToken);
    }

    /// <inheritdoc/>
    public async Task<SQSBatchResponse> ExecuteWithSQSBatchResponseAsync(SQSEvent sqsEvent, CancellationToken stoppingToken, LambdaMessagePollerOptions? options = null)
    {
        if (!sqsEvent.Records.Any())
            return new SQSBatchResponse();

        var lambdaMessagePollerConfiguration = CreatelambdaMessagePollerConfiguration(sqsEvent, isPartialBatchResponseEnabled: true, options);
        var lambdaMessagePoller = _messagePollerFactory.CreateMessagePoller(lambdaMessagePollerConfiguration);
        await lambdaMessagePoller.StartPollingAsync(stoppingToken);
        return lambdaMessagePollerConfiguration.SQSBatchResponse;
    }

    private LambdaMessagePollerConfiguration CreatelambdaMessagePollerConfiguration(SQSEvent sqsEvent, bool isPartialBatchResponseEnabled, LambdaMessagePollerOptions? options = null)
    {
        if (options is null)
        {
            options = new LambdaMessagePollerOptions();
        }
        options.Validate();
        var sqsQueueArn = sqsEvent.Records[0].EventSourceArn;
        var sqsQueueUrl = GetSQSQueueUrl(sqsQueueArn);
        var lambdaMessagePollerConfiguration = new LambdaMessagePollerConfiguration(sqsQueueUrl)
        {
            SQSEvent = sqsEvent,
            IsPartialBatchResponseEnabled = isPartialBatchResponseEnabled,
            // TODO: This will value should be set differently when working with FIFO queues.
            MaxNumberOfConcurrentMessages = options.MaxNumberOfConcurrentMessages
        };

        return lambdaMessagePollerConfiguration;
    }

    private string GetSQSQueueUrl(string queueArn)
    {
        // ARN structure - arn:aws:sqs:{REGION}:{ACCOUNT-ID}:{QUEUE-NAME}
        // URL structure - https://sqs.{REGION}.amazonaws.com/{ACCOUNT-ID}/{QUEUE-NAME}
        var arnSegments = queueArn.Split(':');
        if (arnSegments.Length != 6)
        {
            _logger.LogError("{queueArn} is not a valid SQS queue ARN", queueArn);
            throw new InvalidSQSQueueArnException($"{queueArn} is not a valid SQS queue ARN");
        }
        var region = arnSegments[3];
        var accountId = arnSegments[4];
        var queueName = arnSegments[5];

        var queueUrl = $"https://sqs.{region}.amazonaws.com/{accountId}/{queueName}";
        return queueUrl;
    }
}
