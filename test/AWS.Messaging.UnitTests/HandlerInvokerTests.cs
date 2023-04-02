// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using AWS.Messaging.UnitTests.MessageHandlers;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests;

/// <summary>
/// Tests for <see cref="HandlerInvoker"/>
/// </summary>
public class HandlerInvokerTests
{
    /// <summary>
    /// Tests that a single handler can be invoked successfully
    /// </summary>
    [Fact]
    public async Task HandlerInvoker_HappyPath()
    {
        var serviceCollection = new ServiceCollection()
            .AddAWSMessageBus(builder =>
            {
                builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("sqsQueueUrl");
            });

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var handlerInvoker = new HandlerInvoker(serviceProvider, new NullLogger<HandlerInvoker>(), new DefaultTelemetryWriter(serviceProvider));

        var envelope = new MessageEnvelope<ChatMessage>();
        var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));
        var messageProcessStatus = await handlerInvoker.InvokeAsync(envelope, subscriberMapping);

        Assert.Equal(MessageProcessStatus.Success(), messageProcessStatus);
    }

    /// <summary>
    /// Tests that the correct methods are invoked on a single type that
    /// implements the handler for multiple message types
    /// </summary>
    [Fact]
    public async Task HandlerInvoker_DualHandler_InvokesCorrectMethod()
    {
        var serviceCollection = new ServiceCollection()
            .AddAWSMessageBus(builder =>
            {
                builder.AddMessageHandler<DualHandler, ChatMessage>("sqsQueueUrl");
                builder.AddMessageHandler<DualHandler, AddressInfo>("sqsQueueUrl");
            });

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var handlerInvoker = new HandlerInvoker(serviceProvider, new NullLogger<HandlerInvoker>(), new DefaultTelemetryWriter(serviceProvider));

        // Assert that ChatMessage is routed to the right handler method, which always succeeds
        var chatEnvelope = new MessageEnvelope<ChatMessage>();
        var chatSubscriberMapping = new SubscriberMapping(typeof(DualHandler), typeof(ChatMessage));
        var chatMessageProcessStatus = await handlerInvoker.InvokeAsync(chatEnvelope, chatSubscriberMapping);

        Assert.True(chatMessageProcessStatus.IsSuccess);

        // Assert that AddressInfo is routed to the right handler method, which always fails
        var addressEnvelope = new MessageEnvelope<AddressInfo>();
        var addressSubscriberMapping = new SubscriberMapping(typeof(DualHandler), typeof(AddressInfo));
        var addressMessageProcessStatus = await handlerInvoker.InvokeAsync(addressEnvelope, addressSubscriberMapping);

        Assert.True(addressMessageProcessStatus.IsFailed);
    }

    /// <summary>
    /// Tests that a exception thrown by a handler is logged correctly, since
    /// the handler is invoked via reflection we want to log the inner exception and
    /// not the TargetInvocationException
    /// </summary>
    [Fact]
    public async Task HandlerInvoker_UnwrapsTargetInvocationException()
    {
        var mockLogger = new Mock<ILogger<HandlerInvoker>>();

        var serviceCollection = new ServiceCollection()
            .AddAWSMessageBus(builder =>
            {
                builder.AddMessageHandler<ChatExceptionHandler, ChatMessage>("sqsQueueUrl");
            });

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var handlerInvoker = new HandlerInvoker(serviceProvider, mockLogger.Object, new DefaultTelemetryWriter(serviceProvider));
        var envelope = new MessageEnvelope<ChatMessage>()
        {
            Id = "123"
        };
        var subscriberMapping = new SubscriberMapping(typeof(ChatExceptionHandler), typeof(ChatMessage));

        await handlerInvoker.InvokeAsync(envelope, subscriberMapping);

        mockLogger.VerifyLogError(typeof(CustomHandlerException), "A handler exception occurred while handling message ID 123.");

    }
}
