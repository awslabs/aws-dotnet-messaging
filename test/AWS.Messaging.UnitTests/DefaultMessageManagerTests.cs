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
            var mockPoller = CreateMockPoller();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success());

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration());

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
            var mockPoller = CreateMockPoller();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Failed());

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration());

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
        /// Tests that the manager extends the visibility timeout when the message handler takes longer than the
        /// refresh interval and the message is within the visibility timeout exipration threshold
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_RefreshesLongHandlerWhenNecessary()
        {
            var mockPoller = CreateMockPoller();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), messageHandlingDelay: TimeSpan.FromSeconds(3));

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration
            {
                VisibilityTimeoutExtensionThreshold = 1,
                VisibilityTimeoutExtensionHeartbeatInterval = TimeSpan.FromSeconds(1)
            });

            var expiringMessage = new MessageEnvelope<ChatMessage>()
            {
                Id = "1",
                SQSMetadata = new SQSMetadata()
                {
                    ApproximateFirstReceiveTimestamp = DateTimeOffset.UtcNow,
                    ExpectedVisibilityTimeoutExpiration = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1) // will need to refresh relative to the 3 second handler
                }
            };

            var longerTimeoutMessage = new MessageEnvelope<ChatMessage>()
            {
                Id = "2",
                SQSMetadata = new SQSMetadata()
                {
                    ApproximateFirstReceiveTimestamp = DateTimeOffset.UtcNow,
                    ExpectedVisibilityTimeoutExpiration = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10) // will NOT need to refresh relative to the 3 second handler
                }
            };
            var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

            // Don't await the tasks, but start them so we can assert ActiveMessageCount was incremented while the handler is still processing
            var expiringTask = manager.ProcessMessageAsync(expiringMessage, subscriberMapping);
            var longerTimeoutTask = manager.ProcessMessageAsync(longerTimeoutMessage, subscriberMapping);
            Assert.Equal(2, manager.ActiveMessageCount);

            // Now finish awaiting the handler
            await Task.WhenAll(expiringTask, longerTimeoutTask);

            // Verify that the handler was invoked with the expected messages and mapping
            mockHandlerInvoker.Verify(x => x.InvokeAsync(
                        It.Is<MessageEnvelope>(actualEnvelope => actualEnvelope == expiringMessage),
                        It.Is<SubscriberMapping>(actualMapping => actualMapping == subscriberMapping),
                        It.IsAny<CancellationToken>()),
                Times.Once());
            mockHandlerInvoker.Verify(x => x.InvokeAsync(
                       It.Is<MessageEnvelope>(actualEnvelope => actualEnvelope == longerTimeoutMessage),
                       It.Is<SubscriberMapping>(actualMapping => actualMapping == subscriberMapping),
                       It.IsAny<CancellationToken>()),
               Times.Once());

            // Since the mock handler invoker returns success, verify that delete was called
            mockPoller.Verify(poller => poller.DeleteMessagesAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == expiringMessage),
                    It.IsAny<CancellationToken>()),
                Times.Once());
            mockPoller.Verify(poller => poller.DeleteMessagesAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == longerTimeoutMessage),
                    It.IsAny<CancellationToken>()),
                Times.Once());

            // Since the message handler takes 3 seconds, verify the the visibility was extended
            // TODO: the 2 to 3 allows for some instability
            mockPoller.Verify(poller => poller.ExtendMessageVisibilityTimeoutAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == expiringMessage),
                    It.IsAny<CancellationToken>()),
                Times.Between(2, 3, Moq.Range.Inclusive));

            // Verify that there were no other calls, which is guarding against ExtendMessageVisibilityTimeoutAsync
            // with longerTimeoutTask since we don't expect its visibility timeout to be extended
            mockPoller.VerifyNoOtherCalls();

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
            var mockPoller = CreateMockPoller();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), TimeSpan.FromSeconds(1));

            var manager = new DefaultMessageManager(mockPoller.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration());
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
        /// <returns>Mock poller</returns>
        private Mock<IMessagePoller> CreateMockPoller()
        {
            var mockPoller = new Mock<IMessagePoller>();

            mockPoller.Setup(x => x.ExtendMessageVisibilityTimeoutAsync(
                    It.IsAny<IEnumerable<MessageEnvelope>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

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
