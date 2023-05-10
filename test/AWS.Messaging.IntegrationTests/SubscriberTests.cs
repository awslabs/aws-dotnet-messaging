// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.IntegrationTests.Handlers;
using AWS.Messaging.IntegrationTests.Models;
using AWS.Messaging.Tests.Common.Services;
using AWS.Messaging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AWS.Messaging.IntegrationTests;

public class SubscriberTests : IAsyncLifetime
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceCollection _serviceCollection;
    private string _sqsQueueUrl;

    public SubscriberTests()
    {
        _sqsClient = new AmazonSQSClient();
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddLogging(x => x.AddInMemoryLogger().SetMinimumLevel(LogLevel.Trace));
        _sqsQueueUrl = string.Empty;
    }

    public async Task InitializeAsync()
    {
        var createQueueResponse = await _sqsClient.CreateQueueAsync($"MPFTest-{Guid.NewGuid().ToString().Split('-').Last()}");
        _sqsQueueUrl = createQueueResponse.QueueUrl;
    }

    [Fact]
    public async Task SendAndReceive1Message()
    {
        _serviceCollection.AddSingleton<TempStorage<ChatMessage>>();
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl, options =>
            {
                options.VisibilityTimeoutExtensionThreshold = 3;
            });
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
            builder.AddMessageSource("/aws/messaging");
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publishStartTime = DateTime.UtcNow;
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new ChatMessage
        {
            MessageDescription = "Test1"
        });
        var publishEndTime = DateTime.UtcNow;

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        await pump.StartAsync(source.Token);

        var tempStorage = serviceProvider.GetRequiredService<TempStorage<ChatMessage>>();
        source.CancelAfter(60000);
        while (!source.IsCancellationRequested) { }

        var message = Assert.Single(tempStorage.Messages);
        Assert.False(string.IsNullOrEmpty(message.Id));
        Assert.Equal("/aws/messaging", message.Source.ToString());
        Assert.True(message.TimeStamp > publishStartTime);
        Assert.True(message.TimeStamp < publishEndTime);
        Assert.Equal("Test1", message.Message.MessageDescription);
    }

    [Theory]
    // Tests that the visibility is extended without needing multiple batch requests
    [InlineData(5, 5)]
    // Tests that the visibility is extended with the need for multiple batch requests
    [InlineData(8, 5)]
    // Increasing the number of messages processed to ensure stability at load
    [InlineData(15, 15)]
    // Increasing the number of messages processed with batching required to extend visibility
    [InlineData(20, 15)]
    public async Task SendAndReceiveMultipleMessages(int numberOfMessages, int maxConcurrentMessages)
    {
        _serviceCollection.AddSingleton<TempStorage<ChatMessage>>();
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl, options =>
            {
                options.VisibilityTimeout = 5; // 5s will require a visibility timeout extension due to the 10s handler below
                options.VisibilityTimeoutExtensionThreshold = 3; // and a message is eligible for extension after it's been processing at least 3 seconds
                options.MaxNumberOfConcurrentMessages = maxConcurrentMessages;
            });
            builder.AddMessageHandler<ChatMessageHandler_10sDelay, ChatMessage>();
            builder.AddMessageSource("/aws/messaging");
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publishStartTime = DateTime.UtcNow;
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        for (int i = 0; i < numberOfMessages; i++)
        {
            await publisher.PublishAsync(new ChatMessage
            {
                MessageDescription = $"Test{i + 1}"
            });
        }
        var publishEndTime = DateTime.UtcNow;

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        await pump.StartAsync(source.Token);

        var numberOfBatches = (int)Math.Ceiling((decimal)numberOfMessages / maxConcurrentMessages);

        // Wait for the pump to shut down after processing the expected number of messages,
        // with some padding to ensure messages aren't being processed more than once
        source.CancelAfter(numberOfBatches * 12000);
        while (!source.IsCancellationRequested) { }

        var inMemoryLogger = serviceProvider.GetRequiredService<InMemoryLogger>();
        var tempStorage = serviceProvider.GetRequiredService<TempStorage<ChatMessage>>();

        Assert.Empty(inMemoryLogger.Logs.Where(x => x.Exception is AmazonSQSException ex && ex.ErrorCode.Equals("AWS.SimpleQueueService.TooManyEntriesInBatchRequest")));
        Assert.Equal(numberOfMessages, tempStorage.Messages.Count);
        for (int i = 0; i < numberOfMessages; i++)
        {
            var message = tempStorage.Messages.FirstOrDefault(x => x.Message.MessageDescription.Equals($"Test{i + 1}"));
            Assert.NotNull(message);
            Assert.False(string.IsNullOrEmpty(message.Id));
            Assert.Equal("/aws/messaging", message.Source.ToString());
            Assert.True(message.TimeStamp > publishStartTime);
            Assert.True(message.TimeStamp < publishEndTime);
        }
    }

    [Theory]
    [InlineData(20)]
    public async Task SendMixOfMessageTypesToSameQueue(int numberOfMessages)
    {
        _serviceCollection.AddSingleton<TempStorage<ChatMessage>>();
        _serviceCollection.AddSingleton<TempStorage<FoodItem>>();
        _serviceCollection.AddSingleton<TempStorage<OrderInfo>>();
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPublisher<FoodItem>(_sqsQueueUrl);
            builder.AddSQSPublisher<OrderInfo>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl);
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
            builder.AddMessageHandler<FoodItemHandler, FoodItem>();
            builder.AddMessageHandler<OrderInfoHandler, OrderInfo>();
            builder.AddMessageSource("/aws/messaging");
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publishStartTime = DateTime.UtcNow;
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        for (int i = 0; i < numberOfMessages; i++)
        {
            await publisher.PublishAsync(new ChatMessage
            {
                MessageDescription = $"Test{i + 1}"
            });
            await publisher.PublishAsync(new FoodItem
            {
                Id = i + 1,
                Name = $"User{i + 1}"
            });
            await publisher.PublishAsync(new OrderInfo
            {
                OrderId = $"{i + 1}",
                UserId = $"User{i + 1}"
            });
        }
        var publishEndTime = DateTime.UtcNow;

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        await pump.StartAsync(source.Token);

        var chatMessageTempStorage = serviceProvider.GetRequiredService<TempStorage<ChatMessage>>();
        var foodItemTempStorage = serviceProvider.GetRequiredService<TempStorage<FoodItem>>();
        var orderInfoTempStorage = serviceProvider.GetRequiredService<TempStorage<OrderInfo>>();
        source.CancelAfter(60000);
        while (!source.IsCancellationRequested) { }

        Assert.Equal(numberOfMessages, chatMessageTempStorage.Messages.Count);
        Assert.Equal(numberOfMessages, foodItemTempStorage.Messages.Count);
        Assert.Equal(numberOfMessages, orderInfoTempStorage.Messages.Count);
        for (int i = 0; i < numberOfMessages; i++)
        {
            var chatMessage = chatMessageTempStorage.Messages.FirstOrDefault(x => x.Message.MessageDescription.Equals($"Test{i + 1}"));
            Assert.NotNull(chatMessage);
            Assert.False(string.IsNullOrEmpty(chatMessage.Id));
            Assert.Equal("/aws/messaging", chatMessage.Source.ToString());
            Assert.True(chatMessage.TimeStamp > publishStartTime);
            Assert.True(chatMessage.TimeStamp < publishEndTime);

            var foodItem = foodItemTempStorage.Messages.FirstOrDefault(x => x.Message.Id.Equals(i + 1));
            Assert.NotNull(foodItem);
            Assert.False(string.IsNullOrEmpty(foodItem.Id));
            Assert.Equal("/aws/messaging", foodItem.Source.ToString());
            Assert.True(foodItem.TimeStamp > publishStartTime);
            Assert.True(foodItem.TimeStamp < publishEndTime);


            var orderInfo = orderInfoTempStorage.Messages.FirstOrDefault(x => x.Message.OrderId.Equals($"{i + 1}"));
            Assert.NotNull(orderInfo);
            Assert.False(string.IsNullOrEmpty(orderInfo.Id));
            Assert.Equal("/aws/messaging", orderInfo.Source.ToString());
            Assert.True(orderInfo.TimeStamp > publishStartTime);
            Assert.True(orderInfo.TimeStamp < publishEndTime);
        }
    }

    [Theory]
    [InlineData(20)]
    public async Task MessagesWithFrameworkandWithout(int numberOfMessages)
    {
        _serviceCollection.AddSingleton<TempStorage<ChatMessage>>();
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl);
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
            builder.AddMessageSource("/aws/messaging");
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publishStartTime = DateTime.UtcNow;
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        for (int i = 0; i < numberOfMessages; i++)
        {
            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _sqsQueueUrl,
                MessageBody = "NotUsingFramework"
            });
            await publisher.PublishAsync(new ChatMessage
            {
                MessageDescription = $"UsingFramework"
            });
        }
        var publishEndTime = DateTime.UtcNow;

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        await pump.StartAsync(source.Token);

        var chatMessageTempStorage = serviceProvider.GetRequiredService<TempStorage<ChatMessage>>();
        source.CancelAfter(30000);
        while (!source.IsCancellationRequested) { }

        var inMemoryLogger = serviceProvider.GetRequiredService<InMemoryLogger>();
        var errorMessages = inMemoryLogger.Logs.Where(x => x.Message.Equals("Failed to create a MessageEnvelope"));
        Assert.NotEmpty(errorMessages);
        Assert.True(errorMessages.Count() >= numberOfMessages);
        Assert.Equal(numberOfMessages, chatMessageTempStorage.Messages.Count);
        var chatMessages = chatMessageTempStorage.Messages.Where(x => x.Message.MessageDescription.Equals($"UsingFramework")).ToList();
        foreach (var message in chatMessages)
        {
            Assert.NotNull(message);
            Assert.False(string.IsNullOrEmpty(message.Id));
            Assert.Equal("/aws/messaging", message.Source.ToString());
            Assert.True(message.TimeStamp > publishStartTime);
            Assert.True(message.TimeStamp < publishEndTime);
        }
    }

    [Theory]
    [InlineData(5)]
    public async Task MessagesWithoutHandlers(int numberOfMessages)
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl);
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        for (int i = 0; i < numberOfMessages; i++)
        {
            await publisher.PublishAsync(new ChatMessage
            {
                MessageDescription = $"UsingFramework"
            });
        }

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        var processStartTime = DateTime.UtcNow;
        await pump.StartAsync(source.Token);

        source.CancelAfter(60000);
        while (!source.IsCancellationRequested) { }
        var timeElapsed = DateTime.UtcNow - processStartTime;

        var inMemoryLogger = serviceProvider.GetRequiredService<InMemoryLogger>();
        var errorMessages = inMemoryLogger.Logs.Where(x => x.Message.Equals("_messageConfiguration does not have a valid subscriber mapping for message ID 'AWS.Messaging.IntegrationTests.Models.ChatMessage'"));
        Assert.NotEmpty(errorMessages);
        Assert.True(errorMessages.Count() >= numberOfMessages);
        Assert.True(timeElapsed.TotalSeconds > 59);
        Assert.True(source.IsCancellationRequested);
    }

    [Theory]
    [InlineData(5)]
    public async Task MessagesWithFailedHandlers(int numberOfMessages)
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl);
            builder.AddMessageHandler<ChatMessageHandler_Failed, ChatMessage>();
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        for (int i = 0; i < numberOfMessages; i++)
        {
            await publisher.PublishAsync(new ChatMessage
            {
                MessageDescription = $"UsingFramework"
            });
        }

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        var processStartTime = DateTime.UtcNow;
        await pump.StartAsync(source.Token);

        source.CancelAfter(60000);
        while (!source.IsCancellationRequested) { }
        var timeElapsed = DateTime.UtcNow - processStartTime;

        var inMemoryLogger = serviceProvider.GetRequiredService<InMemoryLogger>();
        var errorMessages = inMemoryLogger.Logs.Where(x => x.Message.Contains("Message handling failed unexpectedly for message"));
        Assert.NotEmpty(errorMessages);
        Assert.True(errorMessages.Count() >= numberOfMessages);
        Assert.True(timeElapsed.TotalSeconds > 59);
        Assert.True(source.IsCancellationRequested);
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
