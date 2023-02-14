// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Text.Json;
using Amazon.EventBridge;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using AWS.Messaging.Services;
using AWS.Messaging.UnitTests.MessageHandlers;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class MessageBusBuilderTests
{
    private readonly IServiceCollection _serviceCollection;

    public MessageBusBuilderTests()
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddLogging();
        _serviceCollection.AddDefaultAWSOptions(new AWSOptions
        {
            Region = Amazon.RegionEndpoint.USWest2
        });
    }

    [Fact]
    public void BuildMessageBus()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<OrderInfo>("sqsQueueUrl");
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messageConfiguration = serviceProvider.GetService<IMessageConfiguration>();
        Assert.NotNull(messageConfiguration);

        var messagePublisher = serviceProvider.GetService<IMessagePublisher>();
        Assert.NotNull(messagePublisher);
    }

    [Fact]
    public void MessageBus_NoBuild_NoServices()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messageConfiguration = serviceProvider.GetService<IMessageConfiguration>();
        Assert.Null(messageConfiguration);

        var messagePublisher = serviceProvider.GetService<IMessagePublisher>();
        Assert.Null(messagePublisher);
    }

    [Fact]
    public void MessageBus_AddSQSQueue()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<OrderInfo>("sqsQueueUrl");
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var sqsClient = serviceProvider.GetService<IAmazonSQS>();
        Assert.NotNull(sqsClient);

        var snsClient = serviceProvider.GetService<IAmazonSimpleNotificationService>();
        Assert.Null(snsClient);

        var eventBridgeClient = serviceProvider.GetService<IAmazonEventBridge>();
        Assert.Null(eventBridgeClient);
    }

    [Fact]
    public void MessageBus_AddSNSTopic()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSNSPublisher<OrderInfo>("snsTopicUrl");
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var snsClient = serviceProvider.GetService<IAmazonSimpleNotificationService>();
        Assert.NotNull(snsClient);

        var sqsClient = serviceProvider.GetService<IAmazonSQS>();
        Assert.Null(sqsClient);

        var eventBridgeClient = serviceProvider.GetService<IAmazonEventBridge>();
        Assert.Null(eventBridgeClient);
    }

    [Fact]
    public void MessageBus_AddEventBus()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddEventBridgePublisher<OrderInfo>("eventBusUrl");
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var eventBridgeClient = serviceProvider.GetService<IAmazonEventBridge>();
        Assert.NotNull(eventBridgeClient);

        var snsClient = serviceProvider.GetService<IAmazonSimpleNotificationService>();
        Assert.Null(snsClient);

        var sqsClient = serviceProvider.GetService<IAmazonSQS>();
        Assert.Null(sqsClient);
    }

    [Fact]
    public void MessageBus_AddMessageHandler()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("sqsQueueUrl");
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messageHandler = serviceProvider.GetService<ChatMessageHandler>();
        Assert.NotNull(messageHandler);
    }

    [Fact]
    public void MessageBus_NoMessageHandler()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messageHandler = serviceProvider.GetService<ChatMessageHandler>();
        Assert.Null(messageHandler);
    }

    [Fact]
    public void MessageBus_MessageSerializerShouldExist()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller("queueUrl");
        });

        _serviceCollection.AddSingleton<ILogger<MessageSerializer>, NullLogger<MessageSerializer>>();

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messageSerializer = serviceProvider.GetService<IMessageSerializer>();
        Assert.NotNull(messageSerializer);
    }

    [Fact]
    public void MessageBus_AddSerializerOptions()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.ConfigureSerializationOptions(options =>
            {
                options.SystemTextJsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
            });
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messageConfiguration = serviceProvider.GetService<IMessageConfiguration>();
        Assert.NotNull(messageConfiguration);

        var jsonSerializerOptions = messageConfiguration.SerializationOptions.SystemTextJsonOptions;
        Assert.NotNull(jsonSerializerOptions);
        Assert.Equal(JsonNamingPolicy.CamelCase, jsonSerializerOptions.PropertyNamingPolicy);
    }

    /// <summary>
    /// Asserts that adding a SQS Poller will add a SQS client and
    /// a MessagePumpService to the service provider.
    /// </summary>
    [Fact]
    public void MessageBus_AddSQSPoller()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller("queueUrl");
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messagePumpService = serviceProvider.GetServices<IHostedService>().OfType<MessagePumpService>().Single();
        Assert.NotNull(messagePumpService);

        var sqsClient = serviceProvider.GetService<IAmazonSQS>();
        Assert.NotNull(sqsClient);
    }
}
