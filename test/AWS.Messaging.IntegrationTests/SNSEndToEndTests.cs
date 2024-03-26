// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using AWS.Messaging.IntegrationTests.Models;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AWS.Messaging.IntegrationTests.Handlers;
using AWS.Messaging.Services;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace AWS.Messaging.IntegrationTests;

public class SNSEndToEndTests : IAsyncLifetime
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly IAmazonSQS _sqsClient;
    private ServiceProvider _serviceProvider;
    private string _snsTopicArn;
    private string _sqsQueueUrl;

    public SNSEndToEndTests()
    {
        _sqsClient = new AmazonSQSClient();
        _snsClient = new AmazonSimpleNotificationServiceClient();
        _serviceProvider = default!;
        _snsTopicArn = string.Empty;
        _sqsQueueUrl = string.Empty;
    }

    public async Task InitializeAsync()
    {
        var resourceName = $"MPFTest-{Guid.NewGuid().ToString().Split('-').Last()}";
        var createQueueResponse = await _sqsClient.CreateQueueAsync(resourceName);
        _sqsQueueUrl = createQueueResponse.QueueUrl;

        var createTopicResponse = await _snsClient.CreateTopicAsync(resourceName);
        _snsTopicArn = createTopicResponse.TopicArn;

        await _snsClient.SubscribeQueueAsync(_snsTopicArn, _sqsClient, _sqsQueueUrl);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<TempStorage<ChatMessage>>();
        serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSNSPublisher<ChatMessage>(_snsTopicArn);
            builder.AddMessageSource("/aws/messaging");
            builder.AddSQSPoller(_sqsQueueUrl, options =>
            {
                options.VisibilityTimeoutExtensionThreshold = 3;
            });
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task PublishAndProcessMessage()
    {
        var publishStartTime = DateTime.UtcNow;
        var publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new ChatMessage
        {
            MessageDescription = "Test1"
        });
        var publishEndTime = DateTime.UtcNow;

        var pump = _serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        await pump.StartAsync(source.Token);

        var tempStorage = _serviceProvider.GetRequiredService<TempStorage<ChatMessage>>();
        source.CancelAfter(60000);
        while (!source.IsCancellationRequested) { }

        var messageEnvelope = Assert.Single(tempStorage.Messages);
        Assert.False(string.IsNullOrEmpty(messageEnvelope.Id));
        Assert.Equal("/aws/messaging", messageEnvelope.Source.ToString());
        Assert.True(messageEnvelope.TimeStamp > publishStartTime);
        Assert.True(messageEnvelope.TimeStamp < publishEndTime);
        Assert.Equal("Test1", messageEnvelope.Message.MessageDescription);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _snsClient.DeleteTopicAsync(new DeleteTopicRequest { TopicArn = _snsTopicArn });
        }
        catch { }
        try
        {
            await _sqsClient.DeleteQueueAsync(_sqsQueueUrl);
        }
        catch { }
    }
}
