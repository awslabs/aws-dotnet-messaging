// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using AWS.Messaging.UnitTests.MessageHandlers;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class DefaultMessageManagerTests
{
    private const string TEST_QUEUE_URL = "queueUrl";
    private const int TEST_VISIBILITY_TIMEOUT = 10;

    /// <summary>
    /// Happy path test for a successful message handling, that it is deleted at at the end
    /// </summary>
    [Fact]
    public async Task DefaultMessageManager_ManagesMessageSuccess()
    {
        var sqsPollerConfiguration = new SQSMessagePollerConfiguration(TEST_QUEUE_URL){ VisibilityTimeout = TEST_VISIBILITY_TIMEOUT };
        var mockSQSHandler = CreateMockSQSHandler();
        var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success());

        var manager = new DefaultMessageManager(mockSQSHandler.Object, mockHandlerInvoker.Object, sqsPollerConfiguration, new NullLogger<DefaultMessageManager>());

        var messsageEnvelope = new MessageEnvelope<ChatMessage>();
        var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

        await manager.ProcessMessageAsync(messsageEnvelope, subscriberMapping);

        // Verify that the handler was invoked once with the expected message and mapping
        mockHandlerInvoker.Verify(x => x.InvokeAsync(
                It.Is<MessageEnvelope>(actualEnvelope => actualEnvelope == messsageEnvelope),
                It.Is<SubscriberMapping>(actualMapping => actualMapping == subscriberMapping),
                It.IsAny<CancellationToken>()),
            Times.Once());

        // Since the mock handler invoker returns success, verify that delete was called
        mockSQSHandler.Verify(handler => handler.DeleteMessagesAsync(
                It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == messsageEnvelope),
                TEST_QUEUE_URL,
                It.IsAny<CancellationToken>()),
            Times.Once());

        // Since the handler succeeds right away, verify the visiblity was never extended
        mockSQSHandler.Verify(handler => handler.ExtendMessageVisibilityTimeoutAsync(
                It.IsAny<IEnumerable<MessageEnvelope>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never());

        // Verify that the active message count was deprecated back to 0
        Assert.Equal(0, manager.ActiveMessageCount);
    }

    [Fact]
    public async Task DefaultMessageManager_ManagesMessageFailed()
    {
        var sqsPollerConfiguration = new SQSMessagePollerConfiguration(TEST_QUEUE_URL) { VisibilityTimeout = TEST_VISIBILITY_TIMEOUT };
        var mockSQSHandler = CreateMockSQSHandler();
        var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Failed());

        var manager = new DefaultMessageManager(mockSQSHandler.Object, mockHandlerInvoker.Object, sqsPollerConfiguration, new NullLogger<DefaultMessageManager>());

        var messsageEnvelope = new MessageEnvelope<ChatMessage>();
        var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

        await manager.ProcessMessageAsync(messsageEnvelope, subscriberMapping);

        // Verify that the handler was invoked once with the expected message and mapping
        mockHandlerInvoker.Verify(x => x.InvokeAsync(
                It.Is<MessageEnvelope>(actualEnvelope => actualEnvelope == messsageEnvelope),
                It.Is<SubscriberMapping>(actualMapping => actualMapping == subscriberMapping),
                It.IsAny<CancellationToken>()),
            Times.Once());

        // Since the message handling failed, verify that delete was never called
        mockSQSHandler.Verify(handler => handler.DeleteMessagesAsync(
                It.IsAny<IEnumerable<MessageEnvelope>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never());

        // Since the handler fails right away, verify the visiblity was never extended
        mockSQSHandler.Verify(handler => handler.ExtendMessageVisibilityTimeoutAsync(
                It.IsAny<IEnumerable<MessageEnvelope>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never());

        // Verify that the active message count was deprecated back to 0
        Assert.Equal(0, manager.ActiveMessageCount);
    }

    [Fact]
    public async Task DefaultMessageManager_RefreshesLongHandler()
    {
        var sqsPollerConfiguration = new SQSMessagePollerConfiguration(TEST_QUEUE_URL) { VisibilityTimeoutExtensionInterval = 1 };
        var mockSQSHandler = CreateMockSQSHandler();
        var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), messageHandlingDelay: TimeSpan.FromSeconds(3));

        var manager = new DefaultMessageManager(mockSQSHandler.Object, mockHandlerInvoker.Object, sqsPollerConfiguration, new NullLogger<DefaultMessageManager>());

        var messsageEnvelope = new MessageEnvelope<ChatMessage>();
        var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

        // Don't await the task, but start it so we can assert ActiveMessageCount was incremented while the handler is still processing
        var task = manager.ProcessMessageAsync(messsageEnvelope, subscriberMapping);
        Assert.Equal(1, manager.ActiveMessageCount);

        // Now finish awaiting the handler
        await task;

        // Verify that the handler was invoked once with the expected message and mapping
        mockHandlerInvoker.Verify(x => x.InvokeAsync(
                    It.Is<MessageEnvelope>(actualEnvelope => actualEnvelope == messsageEnvelope),
                    It.Is<SubscriberMapping>(actualMapping => actualMapping == subscriberMapping),
                    It.IsAny<CancellationToken>()),
            Times.Once());

        // Since the mock handler invoker returns success, verify that delete was called
        mockSQSHandler.Verify(handler => handler.DeleteMessagesAsync(
                It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == messsageEnvelope),
                TEST_QUEUE_URL,
                It.IsAny<CancellationToken>()),
            Times.Once());

        // Since the message handler takes 3 seconds, verify the the visibility was extended
        // TODO: the 2 to 3 allows for some instability
        mockSQSHandler.Verify(handler => handler.ExtendMessageVisibilityTimeoutAsync(
                It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == messsageEnvelope),
                TEST_QUEUE_URL,
                sqsPollerConfiguration.VisibilityTimeout,
                It.IsAny<CancellationToken>()),
            Times.Between(2, 3, Moq.Range.Inclusive));

        // Verify that the active message count was deprecated back to 0
        Assert.Equal(0, manager.ActiveMessageCount);
    }

    /// <summary>
    /// Mocks the ISQSHandler with the given visibility refresh interval
    /// </summary>
    /// <returns>Mock SQS handler</returns>
    private Mock<ISQSHandler> CreateMockSQSHandler()
    {
        var sqsHandler = new Mock<ISQSHandler>();

        sqsHandler.Setup(x => x.ExtendMessageVisibilityTimeoutAsync(
                It.IsAny<IEnumerable<MessageEnvelope>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return sqsHandler;
    }

    /// <summary>
    /// Mocks a handler invoker that will always return the given status with an optional delay
    /// </summary>
    /// <param name="messageProcessStatus">Message status the handler should always return</param>
    /// <param name="messageHandlingDelay">Optional delay during handling, leave null to return immediately</param>
    /// <returns>Mock handler invoker</returns>
    private Mock<IHandlerInvoker> CreateMockHandlerInvoker(MessageProcessStatus messageProcessStatus, TimeSpan? messageHandlingDelay = null)
    {
        var mockHandlerInvoker = new Mock<IHandlerInvoker>();

        // Moq doesn't support passing in null or TimeSpan.Zero (the default value) for the delay, so branch
        if (messageHandlingDelay != null)
        {
            mockHandlerInvoker.Setup(x => x.InvokeAsync(
                It.IsAny<MessageEnvelope>(),
                It.IsAny<SubscriberMapping>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageProcessStatus, (TimeSpan)messageHandlingDelay);
        }
        else // return the status immediately
        {
            mockHandlerInvoker.Setup(x => x.InvokeAsync(
                It.IsAny<MessageEnvelope>(),
                It.IsAny<SubscriberMapping>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageProcessStatus);
        }

        return mockHandlerInvoker;
    }
}
