// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using AWS.Messaging.Publishers;
using System.Threading.Tasks;
using AWS.Messaging.UnitTests.Models;
using Amazon.SQS;
using AWS.Messaging.Serialization;
using System.Threading;
using Amazon.SQS.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using AWS.Messaging.Publishers.EventBridge;
using AWS.Messaging.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Messaging.UnitTests;

public class MessagePublisherTests
{
    private readonly Mock<IMessageConfiguration> _messageConfiguration;
    private readonly Mock<ILogger<IMessagePublisher>> _logger;
    private readonly Mock<IAmazonSQS> _sqsClient;
    private readonly Mock<IAmazonSimpleNotificationService> _snsClient;
    private readonly Mock<IAmazonEventBridge> _eventBridgeClient;
    private readonly Mock<IEnvelopeSerializer> _envelopeSerializer;
    private readonly ChatMessage _chatMessage;

    public MessagePublisherTests()
    {
        _messageConfiguration = new Mock<IMessageConfiguration>();
        _logger = new Mock<ILogger<IMessagePublisher>>();
        _sqsClient = new Mock<IAmazonSQS>();
        _snsClient = new Mock<IAmazonSimpleNotificationService>();
        _eventBridgeClient = new Mock<IAmazonEventBridge>();
        _envelopeSerializer = new Mock<IEnvelopeSerializer>();

        _envelopeSerializer.SetReturnsDefault<ValueTask<MessageEnvelope<ChatMessage>>> (ValueTask.FromResult(new MessageEnvelope<ChatMessage>()
        {
            Id = "1234",
            Source = new Uri("/aws/messaging/unittest", UriKind.Relative)
        }));


        _chatMessage = new ChatMessage { MessageDescription = "Test Description" };
    }

    [Fact]
    public async Task SQSPublisher_HappyPath()
    {
        var serviceProvider = SetupSQSPublisherDIServices();

        _sqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await messagePublisher.PublishAsync(_chatMessage);

        _sqsClient.Verify(x =>
            x.SendMessageAsync(
                It.Is<SendMessageRequest>(request =>
                    request.QueueUrl.Equals("endpoint")),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task SQSPublisher_MappingNotFound()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await Assert.ThrowsAsync<MissingMessageTypeConfigurationException>(() => messagePublisher.PublishAsync(_chatMessage));
    }

    [Fact]
    public async Task SQSPublisher_InvalidMessage()
    {
        var serviceProvider = SetupSQSPublisherDIServices();

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await Assert.ThrowsAsync<InvalidMessageException>(() => messagePublisher.PublishAsync<ChatMessage?>(null));
    }

    private IServiceProvider SetupSQSPublisherDIServices()
    {
        var publisherConfiguration = new SQSPublisherConfiguration("endpoint");
        var publisherMapping = new PublisherMapping(typeof(ChatMessage), publisherConfiguration, PublisherTargetType.SQS_PUBLISHER);
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);

        var services = new ServiceCollection();
        services.AddSingleton<IAmazonSQS>(_sqsClient.Object);
        services.AddSingleton<ILogger<IMessagePublisher>>(_logger.Object);
        services.AddSingleton<IMessageConfiguration>(_messageConfiguration.Object);
        services.AddSingleton<IEnvelopeSerializer>(_envelopeSerializer.Object);
        services.AddSingleton<ITelemetryWriter, DefaultTelemetryWriter>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SQSPublisher_UnsupportedPublisher()
    {
        var serviceProvider = SetupSQSPublisherDIServices();

        var publisherMapping = new PublisherMapping(typeof(ChatMessage), null!, "NEW_PUBLISHER");
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await Assert.ThrowsAsync<UnsupportedPublisherException>(() => messagePublisher.PublishAsync(_chatMessage));
    }

    private IServiceProvider SetupSNSPublisherDIServices()
    {
        var publisherConfiguration = new SNSPublisherConfiguration("endpoint");
        var publisherMapping = new PublisherMapping(typeof(ChatMessage), publisherConfiguration, PublisherTargetType.SNS_PUBLISHER);
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);

        var services = new ServiceCollection();
        services.AddSingleton<IAmazonSimpleNotificationService>(_snsClient.Object);
        services.AddSingleton<ILogger<IMessagePublisher>>(_logger.Object);
        services.AddSingleton<IMessageConfiguration>(_messageConfiguration.Object);
        services.AddSingleton<IEnvelopeSerializer>(_envelopeSerializer.Object);
        services.AddSingleton<ITelemetryWriter, DefaultTelemetryWriter>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SNSPublisher_HappyPath()
    {
        var serviceProvider = SetupSNSPublisherDIServices();

        _snsClient.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await messagePublisher.PublishAsync(_chatMessage);

        _snsClient.Verify(x =>
            x.PublishAsync(
                It.Is<PublishRequest>(request =>
                    request.TopicArn.Equals("endpoint")),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task SNSPublisher_InvalidMessage()
    {
        var serviceProvider = SetupSNSPublisherDIServices();

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await Assert.ThrowsAsync<InvalidMessageException>(() => messagePublisher.PublishAsync<ChatMessage?>(null));
    }

    private IServiceProvider SetupEventBridgePublisherDIServices()
    {
        var publisherConfiguration = new EventBridgePublisherConfiguration("endpoint");
        var publisherMapping = new PublisherMapping(typeof(ChatMessage), publisherConfiguration, PublisherTargetType.EVENTBRIDGE_PUBLISHER);
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);

        var services = new ServiceCollection();
        services.AddSingleton<IAmazonEventBridge>(_eventBridgeClient.Object);
        services.AddSingleton<ILogger<IMessagePublisher>>(_logger.Object);
        services.AddSingleton<IMessageConfiguration>(_messageConfiguration.Object);
        services.AddSingleton<IEnvelopeSerializer>(_envelopeSerializer.Object);
        services.AddSingleton<ITelemetryWriter, DefaultTelemetryWriter>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EventBridgePublisher_HappyPath()
    {
        var serviceProvider = SetupEventBridgePublisherDIServices();

        _eventBridgeClient.Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await messagePublisher.PublishAsync(_chatMessage);

        _eventBridgeClient.Verify(x =>
            x.PutEventsAsync(
                It.Is<PutEventsRequest>(request =>
                    request.Entries[0].EventBusName.Equals("endpoint") && request.Entries[0].DetailType.Equals("AWS.Messaging.UnitTests.Models.ChatMessage") && request.Entries[0].Source.Equals("/aws/messaging/unittest")),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task EventBridgePublisher_OptionSource()
    {
        var serviceProvider = SetupEventBridgePublisherDIServices();

        _eventBridgeClient.Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new EventBridgePublisher(
            _eventBridgeClient.Object,
            _logger.Object,
            _messageConfiguration.Object,
            _envelopeSerializer.Object
            );

        await messagePublisher.PublishAsync(_chatMessage, new EventBridgeOptions
        {
            Source = "/aws/custom"
        });

        _eventBridgeClient.Verify(x =>
            x.PutEventsAsync(
                It.Is<PutEventsRequest>(request =>
                    request.Entries[0].EventBusName.Equals("endpoint") && request.Entries[0].DetailType.Equals("AWS.Messaging.UnitTests.Models.ChatMessage") && request.Entries[0].Source.Equals("/aws/custom")),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task EventBridgePublisher_SetOptions()
    {
        SetupEventBridgePublisherDIServices();

        _eventBridgeClient.Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new EventBridgePublisher(
            _eventBridgeClient.Object,
            _logger.Object,
            _messageConfiguration.Object,
            _envelopeSerializer.Object
            );

        DateTimeOffset dateTimeOffset = new DateTimeOffset(2015, 2, 17, 0, 0, 0, TimeSpan.Zero);

        await messagePublisher.PublishAsync(_chatMessage, new EventBridgeOptions
        {
            TraceHeader = "trace-header1",
            Time = dateTimeOffset
        });

        _eventBridgeClient.Verify(x =>
            x.PutEventsAsync(
                It.Is<PutEventsRequest>(request =>
                    request.Entries[0].EventBusName.Equals("endpoint") && request.Entries[0].TraceHeader.Equals("trace-header1") && request.Entries[0].Time.Year == dateTimeOffset.Year),

                It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task EventBridgePublisher_InvalidMessage()
    {
        var serviceProvider = SetupEventBridgePublisherDIServices();

        var messagePublisher = new MessageRoutingPublisher(
            serviceProvider,
            _messageConfiguration.Object,
            _logger.Object,
            new DefaultTelemetryWriter(serviceProvider)
            );

        await Assert.ThrowsAsync<InvalidMessageException>(() => messagePublisher.PublishAsync<ChatMessage?>(null));
    }
}
