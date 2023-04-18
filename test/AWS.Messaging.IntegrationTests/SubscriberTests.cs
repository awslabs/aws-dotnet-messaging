// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using AWS.Messaging.IntegrationTests.Handlers;
using AWS.Messaging.IntegrationTests.Models;
using AWS.Messaging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AWS.Messaging.IntegrationTests;

public class SubscriberTests : IAsyncLifetime
{
    private readonly IAmazonSQS _sqsClient;
    private ServiceProvider _serviceProvider;
    private string _sqsQueueUrl;

    public SubscriberTests()
    {
        _sqsClient = new AmazonSQSClient();
        _serviceProvider = default!;
        _sqsQueueUrl = string.Empty;
    }

    public async Task InitializeAsync()
    {
        var createQueueResponse = await _sqsClient.CreateQueueAsync($"MPFTest-{Guid.NewGuid().ToString().Split('-').Last()}");
        _sqsQueueUrl = createQueueResponse.QueueUrl;

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<TempStorage<ChatMessage>>();
        serviceCollection.AddLogging();
        serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl);
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAndReceive1Message()
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
        while (!source.IsCancellationRequested)
        {
            if (tempStorage.Messages.Count > 0)
            {
                source.Cancel();
                break;
            }
        }

        var message = Assert.Single(tempStorage.Messages);
        Assert.False(string.IsNullOrEmpty(message.Id));
        Assert.Equal("/aws/messaging", message.Source.ToString());
        Assert.True(message.TimeStamp > publishStartTime);
        Assert.True(message.TimeStamp < publishEndTime);
        Assert.Equal("Test1", message.Message.MessageDescription);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _sqsClient.DeleteQueueAsync(_sqsQueueUrl);
        }
        catch { }
    }
}
