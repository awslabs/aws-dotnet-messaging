using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using AWS.Messaging.IntegrationTests.Models;
using System.Text.Json;

namespace AWS.Messaging.IntegrationTests;

public class SQSPublisherTests : IAsyncLifetime
{
    private readonly IAmazonSQS _sqsClient;
    private ServiceProvider _serviceProvider;
    private string _sqsQueueUrl;

    public SQSPublisherTests()
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
        serviceCollection.AddLogging();
        serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
        });
        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task PublishMessage()
    {
        var publishStartTime = DateTime.UtcNow;
        var publisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new ChatMessage
        {
            MessageDescription = "Test1"
        });
        var publishEndTime = DateTime.UtcNow;

        // Wait to allow the published message to propagate through the system
        await Task.Delay(5000);

        var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(_sqsQueueUrl);
        var message = Assert.Single(receiveMessageResponse.Messages);

        var envelope = JsonSerializer.Deserialize<MessageEnvelope<string>>(message.Body);
        Assert.NotNull(envelope);
        Assert.False(string.IsNullOrEmpty(envelope.Id));
        Assert.Equal("/aws/messaging", envelope.Source.ToString());
        Assert.True(envelope.TimeStamp >  publishStartTime);
        Assert.True(envelope.TimeStamp < publishEndTime);
        var messageType = Type.GetType(envelope.MessageTypeIdentifier);
        Assert.NotNull(messageType);
        var chatMessageObject = JsonSerializer.Deserialize(envelope.Message, messageType);
        var chatMessage = Assert.IsType<ChatMessage>(chatMessageObject);
        Assert.Equal("Test1", chatMessage.MessageDescription);
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
