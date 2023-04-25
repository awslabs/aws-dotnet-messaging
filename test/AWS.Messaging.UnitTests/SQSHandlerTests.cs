// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Services;
using AWS.Messaging.UnitTests.MessageHandlers;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class SQSHandlerTests
{
    private const string TEST_QUEUE_URL = "queueUrl";

    /// <summary>
    /// Tests that calling <see cref="ISQSHandler.DeleteMessagesAsync"/> calls
    /// SQS's DeleteMessageBatch with an expected request.
    /// </summary>
    [Fact]
    public async Task SQSHandler_DeleteMessages_Success()
    {
        var client = new Mock<IAmazonSQS>();

        client.Setup(x => x.DeleteMessageBatchAsync(It.IsAny<DeleteMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageBatchResponse { Failed = new List<BatchResultErrorEntry>() });

        var sqsHandler = CreateSQSHandler(client);

        var messageEnvelopes = new List<MessageEnvelope>()
        {
            new MessageEnvelope<ChatMessage> { Id = "1", SQSMetadata = new SQSMetadata { ReceiptHandle ="rh1"} },
            new MessageEnvelope<ChatMessage> { Id = "2", SQSMetadata = new SQSMetadata { ReceiptHandle ="rh2"} }
        };

        await sqsHandler.DeleteMessagesAsync(messageEnvelopes, TEST_QUEUE_URL);

        client.Verify(x => x.DeleteMessageBatchAsync(
            It.Is<DeleteMessageBatchRequest>(request =>
                request.QueueUrl == TEST_QUEUE_URL &&
                request.Entries.Count == 2 &&
                request.Entries.Any(entry => entry.Id == "1" && entry.ReceiptHandle == "rh1") &&
                request.Entries.Any(entry => entry.Id == "2" && entry.ReceiptHandle == "rh2")),
            It.IsAny<CancellationToken>()));
    }

    /// <summary>
    /// Tests that calling <see cref="ISQSHandler.ExtendMessageVisibilityTimeoutAsync"/> calls
    /// SQS's ChangeMessageVisibilityBatch with an expected request.
    /// </summary>
    [Fact]
    public async Task SQSMessagePoller_ExtendMessageVisibility_Success()
    {
        var client = new Mock<IAmazonSQS>();

        client.Setup(x => x.ChangeMessageVisibilityBatchAsync(It.IsAny<ChangeMessageVisibilityBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityBatchResponse { Failed = new List<BatchResultErrorEntry>() }, TimeSpan.FromMilliseconds(50));

        var sqsHandler = CreateSQSHandler(client);

        var messageEnvelopes = new List<MessageEnvelope>()
        {
            new MessageEnvelope<ChatMessage> { Id = "1", SQSMetadata = new SQSMetadata { ReceiptHandle ="rh1"} },
            new MessageEnvelope<ChatMessage> { Id = "2", SQSMetadata = new SQSMetadata { ReceiptHandle ="rh2"} }
        };

        await sqsHandler.ExtendMessageVisibilityTimeoutAsync(messageEnvelopes, TEST_QUEUE_URL, 30);

        client.Verify(x => x.ChangeMessageVisibilityBatchAsync(
            It.Is<ChangeMessageVisibilityBatchRequest>(request =>
                request.QueueUrl == TEST_QUEUE_URL &&
                request.Entries.Count == 2 &&
                request.Entries.Any(entry => entry.Id == "1" && entry.ReceiptHandle == "rh1") &&
                request.Entries.Any(entry => entry.Id == "2" && entry.ReceiptHandle == "rh2") &&
                request.Entries.All(entry => entry.VisibilityTimeout == 30)),
            It.IsAny<CancellationToken>()));
    }

    /// <summary>
    /// Helper function that initializes an SQSHandler
    /// </summary>
    /// <param name="mockSqsClient">Mocked SQS client</param>
    private ISQSHandler CreateSQSHandler(Mock<IAmazonSQS> mockSqsClient)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();

        serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller(TEST_QUEUE_URL);
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });

        serviceCollection.AddSingleton(mockSqsClient.Object);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var sqsHandler = serviceProvider.GetService<ISQSHandler>();
        Assert.NotNull(sqsHandler);

        return sqsHandler;
    }
}
