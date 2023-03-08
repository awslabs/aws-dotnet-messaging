// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
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

        var messagePublisher = serviceProvider.GetService<IMessagePublisher>();
        Assert.NotNull(messagePublisher);

        CheckRequiredServices(serviceProvider);
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
    /// Asserts that adding a SQS Poller will add the required 
    /// factories and services to the service provider
    /// </summary>
    [Fact]
    public void MessageBus_AddSQSPoller()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller("queueUrl");
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        // Verify that a singleton MessagePumpService was added
        var messagePumpService = serviceProvider.GetServices<IHostedService>().OfType<MessagePumpService>().Single();
        Assert.NotNull(messagePumpService);

        // Verify that an SQS client was added
        var sqsClient = serviceProvider.GetService<IAmazonSQS>();
        Assert.NotNull(sqsClient);

        // Verify that the default factories used for subscribing to messages were added
        var messageManagerFactory = serviceProvider.GetService<IMessageManagerFactory>();
        Assert.NotNull(messageManagerFactory);
        Assert.IsType<DefaultMessageManagerFactory>(messageManagerFactory);

        var messagePollerFactory = serviceProvider.GetService<IMessagePollerFactory>();
        Assert.NotNull(messagePollerFactory);
        Assert.IsType<DefaultMessagePollerFactory>(messagePollerFactory);

        // Verify that the message framework configuration object exists
        var messageConfiguration = serviceProvider.GetService<IMessageConfiguration>();
        Assert.NotNull(messageConfiguration);
        Assert.Single(messageConfiguration.MessagePollerConfigurations);

        // ...and contains a single poller configuration
        var configuration = messageConfiguration.MessagePollerConfigurations[0];
        Assert.NotNull(configuration);

        // ...of the expected type, with expected default parameters
        if (configuration is SQSMessagePollerConfiguration sqsConfiguration)
        {
            Assert.Equal("queueUrl", sqsConfiguration.SubscriberEndpoint);
            Assert.Equal(10, sqsConfiguration.MaxNumberOfConcurrentMessages);
        }
        else
        {
            Assert.Fail($"Expected configuration to be of type {typeof(SQSMessagePollerConfiguration)}");
        }

        CheckRequiredServices(serviceProvider);
    }

    /// <summary>
    /// Slimmer variation on <see cref="MessageBus_AddSQSPoller"/> that tests AddSQSPoller
    /// with a non-default values for the SQS options
    [Fact]
    public void MessageBus_AddSQSPoller_NonDefaultOptions()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller("queueUrl", options => {
                options.MaxNumberOfConcurrentMessages = 20;
                options.VisibilityTimeout = 5;
                options.WaitTimeSeconds = 10;
            });
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var messageConfiguration = serviceProvider.GetService<IMessageConfiguration>();
        Assert.NotNull(messageConfiguration);
        Assert.Single(messageConfiguration.MessagePollerConfigurations);

        var configuration = messageConfiguration.MessagePollerConfigurations[0];
        Assert.NotNull(configuration);

        if (configuration is SQSMessagePollerConfiguration sqsConfiguration)
        {
            Assert.Equal("queueUrl", sqsConfiguration.SubscriberEndpoint);
            Assert.Equal(20, sqsConfiguration.MaxNumberOfConcurrentMessages);
            Assert.Equal(5, sqsConfiguration.VisibilityTimeout);
            Assert.Equal(10, sqsConfiguration.WaitTimeSeconds);
        }
        else
        {
            Assert.Fail($"Expected configuration to be of type {typeof(SQSMessagePollerConfiguration)}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="SQSMessagePollerConfiguration"/> and
    /// <see cref="SQSMessagePollerOptions"/> are kept in sync.
    /// </summary>
    [Fact]
    public void SQSMessagePollerConfiguration_SQSMessagePollerOptions_InSync()
    {
        var internalConfigurationMembers = typeof(SQSMessagePollerConfiguration).GetProperties();
        var publicConfigurationMembers = typeof(SQSMessagePollerOptions).GetProperties();

        // The expected difference of 1 is for the queueURL property, which exists in the internal configuration
        // but not the public options because it is required to be set via the constructor.
        if (internalConfigurationMembers.Count() - 1 != publicConfigurationMembers.Count())
        {
            Assert.Fail($"There is a mismatch in the number of properties on {nameof(SQSMessagePollerConfiguration)} and {nameof(SQSMessagePollerOptions)}. " +
                $"Ensure that new public properties to configure SQS polling are added to both classes, " +
                $"and then is set appropriately in {nameof(MessageBusBuilder.AddSQSPoller)} in {nameof(MessageBusBuilder)}.");
        }
    }

    /// <summary>
    /// Test cases for <see cref="SQSMessagePollerConfiguration_SQSMessagePollerOptions_Invalid"/>
    /// </summary>
    public static IEnumerable<object[]> GetInvalidSQSMessagePollerOptionsCases()
    {
        // Must be postive 
        yield return new object[] { new Action<SQSMessagePollerOptions>((options) => options.MaxNumberOfConcurrentMessages = -1) };
        yield return new object[] { new Action<SQSMessagePollerOptions>((options) => options.MaxNumberOfConcurrentMessages = 0) };

        // Must be between 0 seconds and 12 hours inclusive
        yield return new object[] { new Action<SQSMessagePollerOptions>((options) => options.VisibilityTimeout = -1) };
        yield return new object[] { new Action<SQSMessagePollerOptions>((options) => options.VisibilityTimeout = (int)TimeSpan.FromHours(12).TotalSeconds + 1) };

        // Must be between 0 and 20 seconds inclusive
        yield return new object[] { new Action<SQSMessagePollerOptions>((options) => options.WaitTimeSeconds = -1) };
        yield return new object[] { new Action<SQSMessagePollerOptions>((options) => options.WaitTimeSeconds = 21) };
    }

    /// <summary>
    /// Tests that an exception is thrown when configuring the SQS poller with invalid options
    /// </summary>
    /// <param name="options">Action that returns SQSMessagePollerOptions</param>
    [Theory]
    [MemberData(nameof(GetInvalidSQSMessagePollerOptionsCases))]
    public void SQSMessagePollerConfiguration_SQSMessagePollerOptions_Invalid(Action<SQSMessagePollerOptions> options)
    {
        Assert.Throws<InvalidSQSMessagePollerOptionsException>(() =>
            _serviceCollection.AddAWSMessageBus(builder =>
            {
                builder.AddSQSPoller("queueUrl", options);
            }));
    }

    // These services must be present irrespective of whether publishers or subscribers are configured.
    private void CheckRequiredServices(ServiceProvider serviceProvider)
    {
        var messageConfiguration = serviceProvider.GetService<IMessageConfiguration>();
        Assert.NotNull(messageConfiguration);

        var messageSerializer = serviceProvider.GetService<IMessageSerializer>();
        Assert.NotNull(messageSerializer);

        var envelopeSerializer = serviceProvider.GetService<IEnvelopeSerializer>();
        Assert.NotNull(envelopeSerializer);

        var dateTimeHandler = serviceProvider.GetService<IDateTimeHandler>();
        Assert.NotNull(dateTimeHandler);

        var messageIdGenerator = serviceProvider.GetService<IMessageIdGenerator>();
        Assert.NotNull(messageIdGenerator);
    }
}
