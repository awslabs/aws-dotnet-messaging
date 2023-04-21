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

namespace AWS.Messaging.UnitTests
{
    public class DefaultMessageManagerTests
    {
        /// <summary>
        /// Happy path test for a successful message handling, that it is deleted at at the end
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_ManagesMessageSuccess()
        {
            var mockPoller = CreateMockPoller(messageVisibilityRefreshInterval: 10);
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success());

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>());

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
            mockPoller.Verify(poller => poller.DeleteMessagesAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == messsageEnvelope),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            // Since the handler succeeds right away, verify the visiblity was never extended
            mockPoller.Verify(poller => poller.ExtendMessageVisibilityTimeoutAsync(
                    It.IsAny<IEnumerable<MessageEnvelope>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never());

            // Verify that the active message count was deprecated back to 0
            Assert.Equal(0, manager.ActiveMessageCount);
        }

        /// <summary>
        /// Happy path test for a failed message handling, that it is not deleted from the queue at the end
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_ManagesMessageFailed()
        {
            var mockPoller = CreateMockPoller(messageVisibilityRefreshInterval: 10);
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Failed());

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>());

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
            mockPoller.Verify(poller => poller.DeleteMessagesAsync(
                    It.IsAny<IEnumerable<MessageEnvelope>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never());

            // Since the handler fails right away, verify the visiblity was never extended
            mockPoller.Verify(poller => poller.ExtendMessageVisibilityTimeoutAsync(
                    It.IsAny<IEnumerable<MessageEnvelope>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never());

            // Verify that the active message count was deprecated back to 0
            Assert.Equal(0, manager.ActiveMessageCount);
        }

        /// <summary>
        /// Tests that the manager extends the visibility timeout when the message handler
        /// takes longer than the refresh interval
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_RefreshesLongHandler()
        {
            var mockPoller = CreateMockPoller(messageVisibilityRefreshInterval: 1);
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), messageHandlingDelay: TimeSpan.FromSeconds(3));

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>());

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
            mockPoller.Verify(poller => poller.DeleteMessagesAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == messsageEnvelope),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            // Since the message handler takes 3 seconds, verify the the visibility was extended
            // TODO: the 2 to 3 allows for some instability
            mockPoller.Verify(poller => poller.ExtendMessageVisibilityTimeoutAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == messsageEnvelope),
                    It.IsAny<CancellationToken>()),
                Times.Between(2, 3, Moq.Range.Inclusive));

            // Verify that the active message count was deprecated back to 0
            Assert.Equal(0, manager.ActiveMessageCount);
        }

        /// <summary>
        /// Queues many message handling tasks for a single message manager to ensure that it
        /// is managing its active message count in a thread safe manner
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_CountsActiveMessagesCorrectly()
        {
            var mockPoller = CreateMockPoller(messageVisibilityRefreshInterval: 1);
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), TimeSpan.FromSeconds(1));

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>());
            var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));
           
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var messsageEnvelope = new MessageEnvelope<ChatMessage>()
                {
                    Id = i.ToString()
                };
                tasks.Add(manager.ProcessMessageAsync(messsageEnvelope, subscriberMapping));
            }

            await Task.WhenAll(tasks);

            // Verify that the active message count was deprecated back to 0
            Assert.Equal(0, manager.ActiveMessageCount);
        }

        /// <summary>
        /// Mocks a message poller with the given visibility refresh interval
        /// </summary>
        /// <param name="messageVisibilityRefreshInterval">How frequently the manager should extend message visibility in seconds</param>
        /// <returns>Mock poller</returns>
        private Mock<IMessagePoller> CreateMockPoller(int messageVisibilityRefreshInterval)
        {
            var mockPoller = new Mock<IMessagePoller>();

            mockPoller.Setup(x => x.ExtendMessageVisibilityTimeoutAsync(
                    It.IsAny<IEnumerable<MessageEnvelope>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockPoller.Setup(x => x.VisibilityTimeoutExtensionInterval).Returns(messageVisibilityRefreshInterval);

            return mockPoller;
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
}
