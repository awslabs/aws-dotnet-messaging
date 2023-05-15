// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using AWS.Messaging.SQS;
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
            var mockSQSMessageCommunication = CreateMockSQSMessageCommunication();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success());

            var manager = new DefaultMessageManager(mockSQSMessageCommunication.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration());

            var messsageEnvelope = new MessageEnvelope<ChatMessage> { Id = "1" };
            var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

            await manager.ProcessMessageAsync(messsageEnvelope, subscriberMapping);

            // Verify that the handler was invoked once with the expected message and mapping
            mockHandlerInvoker.VerifyInvokeAsyncWasCalledWith(messsageEnvelope, subscriberMapping, Times.Once());

            // Since the mock handler invoker returns success, verify that delete was called
            mockSQSMessageCommunication.VerifyDeleteMessagesAsyncWasCalledWith(messsageEnvelope, Times.Once());

            // Since the handler succeeds right away, verify the visiblity was never extended
            mockSQSMessageCommunication.VerifyExtendMessageVisibilityTimeoutAsync(new[] { messsageEnvelope }, Times.Never());

            // Verify that the active message count was deprecated back to 0
            Assert.Equal(0, manager.ActiveMessageCount);

            // Verify that there were no expected poller/handler calls
            mockSQSMessageCommunication.VerifyNoOtherCalls();
            mockHandlerInvoker.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Happy path test for a failed message handling, that it is not deleted from the queue at the end
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_ManagesMessageFailed()
        {
            var mockSQSMessageCommunication = CreateMockSQSMessageCommunication();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Failed());

            var manager = new DefaultMessageManager(mockSQSMessageCommunication.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration());

            var messsageEnvelope = new MessageEnvelope<ChatMessage> { Id = "1" };
            var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

            await manager.ProcessMessageAsync(messsageEnvelope, subscriberMapping);

            // Verify that the handler was invoked once with the expected message and mapping
            mockHandlerInvoker.VerifyInvokeAsyncWasCalledWith(messsageEnvelope, subscriberMapping, Times.Once());

            // Verify that the handler was invoked once with the expected message and mapping
            mockSQSMessageCommunication.VerifyReportMessageFailureAsync(messsageEnvelope, Times.Once());

            // Since the message handling failed, verify that delete was never called
            mockSQSMessageCommunication.VerifyDeleteMessagesAsyncWasCalledWith(messsageEnvelope, Times.Never());

            // Since the handler fails right away, verify the visiblity was never extended
            mockSQSMessageCommunication.VerifyExtendMessageVisibilityTimeoutAsync(new[] { messsageEnvelope }, Times.Never());

            // Verify that the active message count was deprecated back to 0
            Assert.Equal(0, manager.ActiveMessageCount);

            // Verify that there were no expected poller/handler calls
            mockSQSMessageCommunication.VerifyNoOtherCalls();
            mockHandlerInvoker.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Tests that the manager extends the visibility timeout when appropriate for a batch of in flight messages
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_ExtendsVisibilityTimeout_Batch()
        {
            var mockSQSMessageCommunication = CreateMockSQSMessageCommunication();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), messageHandlingDelay: TimeSpan.FromSeconds(3));

            var manager = new DefaultMessageManager(mockSQSMessageCommunication.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration
            {
                VisibilityTimeout = 2,
                VisibilityTimeoutExtensionThreshold = 1,
                VisibilityTimeoutExtensionHeartbeatInterval = TimeSpan.FromSeconds(1)
            });

            var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

            // Start handling two messages at roughly the same time
            var message1 = new MessageEnvelope<ChatMessage>() { Id = "1" };
            var message2 = new MessageEnvelope<ChatMessage>() { Id = "2" };

            var message1Task = manager.ProcessMessageAsync(message1, subscriberMapping);
            var message2Task = manager.ProcessMessageAsync(message2, subscriberMapping);

            // Don't await the tasks yet,so we can assert that ActiveMessageCount was incremented while the handlers are still processing
            Assert.Equal(2, manager.ActiveMessageCount);

            // Now finish awaiting the handlers
            await Task.WhenAll(message1Task, message2Task);

            // Verify that the handler was invoked with the expected messages and mapping
            mockHandlerInvoker.VerifyInvokeAsyncWasCalledWith(message1, subscriberMapping, Times.Once());
            mockHandlerInvoker.VerifyInvokeAsyncWasCalledWith(message2, subscriberMapping, Times.Once());

            // Since the mock handler invoker returns success, verify that delete was called
            mockSQSMessageCommunication.VerifyDeleteMessagesAsyncWasCalledWith(message1, Times.Once());
            mockSQSMessageCommunication.VerifyDeleteMessagesAsyncWasCalledWith(message2, Times.Once());

            // Since the message handler takes 3 seconds, verify that the visibility was extended
            // The 1 to 3 allows for some instability around the second boundaries
            mockSQSMessageCommunication.VerifyExtendMessageVisibilityTimeoutAsync(new[] { message2, message1 }, Times.Between(1, 3, Moq.Range.Inclusive));

            // Verify that there were no other calls, which is guarding against
            // ExtendMessageVisibilityTimeoutAsync being called with only a single message since
            // we expect these to have the same lifecycle
            mockHandlerInvoker.VerifyNoOtherCalls();

            // Verify that the active message count was deprecated back to 0
            Assert.Equal(0, manager.ActiveMessageCount);
        }

        /// <summary>
        /// Tests that the manager extends the visibility timeout when appropriate for in flight messages
        /// that were received at different timestamps
        /// </summary>
        [Fact]
        public async Task DefaultMessageManager_ExtendsVisibilityTimeout_OnlyWhenNecessary()
        {
            var mockSQSMessageCommunication = CreateMockSQSMessageCommunication();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), messageHandlingDelay: TimeSpan.FromSeconds(4));

            var manager = new DefaultMessageManager(mockSQSMessageCommunication.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration
            {
                VisibilityTimeout = 2,
                VisibilityTimeoutExtensionThreshold = 1,
                VisibilityTimeoutExtensionHeartbeatInterval = TimeSpan.FromSeconds(1)
            });

            var subscriberMapping = new SubscriberMapping(typeof(ChatMessageHandler), typeof(ChatMessage));

            // Start handling a single message
            var earlyMessage = new MessageEnvelope<ChatMessage>() { Id = "1" };
            var earlyTask = manager.ProcessMessageAsync(earlyMessage, subscriberMapping);

            // Delay, then start handling another message
            await Task.Delay(TimeSpan.FromSeconds(3));

            var laterMessage = new MessageEnvelope<ChatMessage>() { Id = "2" };
            var laterTask = manager.ProcessMessageAsync(laterMessage, subscriberMapping);

            // Now finish awaiting the handlers
            await Task.WhenAll(earlyTask, laterTask);

            // Verify that the handler was invoked with the expected messages and mapping
            mockHandlerInvoker.VerifyInvokeAsyncWasCalledWith(earlyMessage, subscriberMapping, Times.Once());
            mockHandlerInvoker.VerifyInvokeAsyncWasCalledWith(laterMessage, subscriberMapping, Times.Once());

            // Since the mock handler invoker returns success, verify that delete was called
            mockSQSMessageCommunication.VerifyDeleteMessagesAsyncWasCalledWith(earlyMessage, Times.Once());
            mockSQSMessageCommunication.VerifyDeleteMessagesAsyncWasCalledWith(laterMessage, Times.Once());

            // Since the message handler takes 3 seconds, verify the the visibility was extended
            // The 1 to 3 allows for some instability around the second boundaries
            mockSQSMessageCommunication.VerifyExtendMessageVisibilityTimeoutAsync(new[] { earlyMessage }, Times.Between(1, 3, Moq.Range.Inclusive));
            mockSQSMessageCommunication.VerifyExtendMessageVisibilityTimeoutAsync(new[] { laterMessage }, Times.Between(1, 3, Moq.Range.Inclusive));

            // Verify that there were no other calls, which is guarding against ExtendMessageVisibilityTimeoutAsync
            // being called with both messages since we never expect them to be batched together
            // T  | T+0   | T+1    | T+2    | T+3    | T+4    | T+5    | T + 6  | T + 7  |
            // M1 | Start |        | Extend | Extend | Finish |        |        |        |
            // M2 |       |                 | Start  |        | Extend | Extend | Finish |
            mockSQSMessageCommunication.VerifyNoOtherCalls();
            mockHandlerInvoker.VerifyNoOtherCalls();

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
            var mockSQSMessageCommunication = CreateMockSQSMessageCommunication();
            var mockHandlerInvoker = CreateMockHandlerInvoker(MessageProcessStatus.Success(), TimeSpan.FromSeconds(1));

            var manager = new DefaultMessageManager(mockSQSMessageCommunication.Object, mockHandlerInvoker.Object, new NullLogger<DefaultMessageManager>(), new MessageManagerConfiguration());
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
        private Mock<ISQSMessageCommunication> CreateMockSQSMessageCommunication()
        {
            var mockSQSMessageCommunication = new Mock<ISQSMessageCommunication>();

            mockSQSMessageCommunication.Setup(x => x.ExtendMessageVisibilityTimeoutAsync(
                    It.IsAny<IEnumerable<MessageEnvelope>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mockSQSMessageCommunication;
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

    /// <summary>
    /// Extension methods for mocked <see cref="IMessagePoller"/> and <see cref="IHandlerInvoker"/> objects to be able to verify more concisely above
    /// </summary>
    public static class MockSQSMessagePollerExtensions
    {
        /// <summary>
        /// Verifies that <see cref="IMessagePoller.DeleteMessagesAsync"/> was called with a specified message a specified number of times
        /// </summary>
        public static void VerifyReportMessageFailureAsync(this Mock<ISQSMessageCommunication> mockSQSMessageCommunication, MessageEnvelope expectedMessage, Times times)
        {
            mockSQSMessageCommunication.Verify(sqsMessageCommunication => sqsMessageCommunication.ReportMessageFailureAsync(
                    It.Is<MessageEnvelope>(x => x == expectedMessage),
                    It.IsAny<CancellationToken>()),
                times);
        }

        /// <summary>
        /// Verifies that <see cref="IMessagePoller.DeleteMessagesAsync"/> was called with a specified message a specified number of times
        /// </summary>
        public static void VerifyDeleteMessagesAsyncWasCalledWith(this Mock<ISQSMessageCommunication> mockSQSMessageCommunication, MessageEnvelope expectedMessage, Times times)
        {
            mockSQSMessageCommunication.Verify(sqsMessageCommunication => sqsMessageCommunication.DeleteMessagesAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.Count() == 1 && x.First() == expectedMessage),
                    It.IsAny<CancellationToken>()),
                times);
        }

        /// <summary>
        /// Verifies that <see cref="IHandlerInvoker.InvokeAsync"/> was called with a specified message and mapping a specified number of times
        /// </summary>
        public static void VerifyInvokeAsyncWasCalledWith(this Mock<IHandlerInvoker> mockHandlerInvoker, MessageEnvelope expectedMessage, SubscriberMapping expectedSubscriberMapping, Times times)
        {
            mockHandlerInvoker.Verify(x => x.InvokeAsync(
                    It.Is<MessageEnvelope>(actualEnvelope => actualEnvelope == expectedMessage),
                    It.Is<SubscriberMapping>(actualMapping => actualMapping == expectedSubscriberMapping),
                    It.IsAny<CancellationToken>()),
                times);
        }

        /// <summary>
        /// Verifies that <see cref="IMessagePoller.ExtendMessageVisibilityTimeoutAsync"/> was called with specified message(s) a specified number of times
        /// </summary>
        public static void VerifyExtendMessageVisibilityTimeoutAsync(this Mock<ISQSMessageCommunication> mockSQSMessageCommunication, IEnumerable<MessageEnvelope> messages, Times times)
        {
            mockSQSMessageCommunication.Verify(sqsMessageCommunication => sqsMessageCommunication.ExtendMessageVisibilityTimeoutAsync(
                    It.Is<IEnumerable<MessageEnvelope>>(x => x.ToHashSet().SetEquals(messages.ToHashSet())), // use HashSets becuase the order may differ
                    It.IsAny<CancellationToken>()),
                times);
        }
    }
}
