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

namespace AWS.Messaging.UnitTests;

public class MessagePublisherTests
{
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<IMessageConfiguration> _messageConfiguration;
    private readonly Mock<ILogger<IMessagePublisher>> _logger;
    private readonly Mock<IAmazonSQS> _sqsClient;
    private readonly Mock<IAmazonSimpleNotificationService> _snsClient;
    private readonly Mock<IAmazonEventBridge> _eventBridgeClient;
    private readonly Mock<IEnvelopeSerializer> _envelopeSerializer;
    private readonly ChatMessage _chatMessage;

    public MessagePublisherTests()
    {
        _serviceProvider = new Mock<IServiceProvider>();
        _messageConfiguration = new Mock<IMessageConfiguration>();
        _logger = new Mock<ILogger<IMessagePublisher>>();
        _sqsClient = new Mock<IAmazonSQS>();
        _snsClient = new Mock<IAmazonSimpleNotificationService>();
        _eventBridgeClient = new Mock<IAmazonEventBridge>();
        _envelopeSerializer = new Mock<IEnvelopeSerializer>();
        _chatMessage = new ChatMessage { MessageDescription = "Test Description" };
    }

    [Fact]
    public async Task SQSPublisher_HappyPath()
    {
        SetupSQSPublisherDIServices();

        _sqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
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
        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
            );

        await Assert.ThrowsAsync<MissingMessageTypeConfigurationException>(() => messagePublisher.PublishAsync(_chatMessage));
    }

    [Fact]
    public async Task SQSPublisher_InvalidMessage()
    {
        SetupSQSPublisherDIServices();

        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
            );

        await Assert.ThrowsAsync<InvalidMessageException>(() => messagePublisher.PublishAsync<ChatMessage?>(null));
    }

    private void SetupSQSPublisherDIServices()
    {
        var publisherConfiguration = new SQSPublisherConfiguration("endpoint");
        var publisherMapping = new PublisherMapping(typeof(ChatMessage), publisherConfiguration, PublisherTargetType.SQS_PUBLISHER);

        _serviceProvider.Setup(x => x.GetService(typeof(IAmazonSQS))).Returns(_sqsClient.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(ILogger<IMessagePublisher>))).Returns(_logger.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IMessageConfiguration))).Returns(_messageConfiguration.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IEnvelopeSerializer))).Returns(_envelopeSerializer.Object);
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);
    }

    [Fact]
    public async Task SQSPublisher_UnsupportedPublisher()
    {
        var publisherMapping = new PublisherMapping(typeof(ChatMessage), null!, "NEW_PUBLISHER");
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);

        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
            );

        await Assert.ThrowsAsync<UnsupportedPublisherException>(() => messagePublisher.PublishAsync(_chatMessage));
    }

    private void SetupSNSPublisherDIServices()
    {
        var publisherConfiguration = new SNSPublisherConfiguration("endpoint");
        var publisherMapping = new PublisherMapping(typeof(ChatMessage), publisherConfiguration, PublisherTargetType.SNS_PUBLISHER);

        _serviceProvider.Setup(x => x.GetService(typeof(IAmazonSimpleNotificationService))).Returns(_snsClient.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(ILogger<IMessagePublisher>))).Returns(_logger.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IMessageConfiguration))).Returns(_messageConfiguration.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IEnvelopeSerializer))).Returns(_envelopeSerializer.Object);
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);
    }

    [Fact]
    public async Task SNSPublisher_HappyPath()
    {
        SetupSNSPublisherDIServices();

        _snsClient.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
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
        SetupSNSPublisherDIServices();

        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
            );

        await Assert.ThrowsAsync<InvalidMessageException>(() => messagePublisher.PublishAsync<ChatMessage?>(null));
    }

    private void SetupEventBridgePublisherDIServices()
    {
        var publisherConfiguration = new EventBridgePublisherConfiguration("endpoint");
        var publisherMapping = new PublisherMapping(typeof(ChatMessage), publisherConfiguration, PublisherTargetType.EVENTBRIDGE_PUBLISHER);

        _serviceProvider.Setup(x => x.GetService(typeof(IAmazonEventBridge))).Returns(_eventBridgeClient.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(ILogger<IMessagePublisher>))).Returns(_logger.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IMessageConfiguration))).Returns(_messageConfiguration.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IEnvelopeSerializer))).Returns(_envelopeSerializer.Object);
        _messageConfiguration.Setup(x => x.GetPublisherMapping(typeof(ChatMessage))).Returns(publisherMapping);
    }
    
    [Fact]
    public async Task EventBridgePublisher_HappyPath()
    {
        SetupEventBridgePublisherDIServices();

        _eventBridgeClient.Setup(x => x.PutEventsAsync(It.IsAny<PutEventsRequest>(), It.IsAny<CancellationToken>()));

        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
            );

        await messagePublisher.PublishAsync(_chatMessage);

        _eventBridgeClient.Verify(x =>
            x.PutEventsAsync(
                It.Is<PutEventsRequest>(request =>
                    request.Entries[0].EventBusName.Equals("endpoint")),
                It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task EventBridgePublisher_InvalidMessage()
    {
        SetupEventBridgePublisherDIServices();

        var messagePublisher = new MessageRoutingPublisher(
            _serviceProvider.Object,
            _messageConfiguration.Object,
            _logger.Object
            );

        await Assert.ThrowsAsync<InvalidMessageException>(() => messagePublisher.PublishAsync<ChatMessage?>(null));
    }
}
