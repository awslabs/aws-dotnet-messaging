// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace AWS.Messaging.UnitTests;

/// <summary>
/// Unit tests for <see cref="MessagePumpService"/>
/// </summary>
public class MessagePumpServiceTests
{
    /// <summary>
    /// Tests that <see cref="MessagePumpService"/> calls CreateMessagePoller
    /// for each configured <see cref="SQSMessagePollerConfiguration"/>
    /// </summary>
    [Fact]
    public void MessagePumpService_CreatesMessagePollers()
    {
        var queueA = new SQSMessagePollerConfiguration("queueA");
        var queueB = new SQSMessagePollerConfiguration("queueB");

        var configuration = new MessageConfiguration();
        configuration.MessagePollerConfigurations = new List<IMessagePollerConfiguration>() { queueA, queueB };

        var messagePollerFactoryMock = new Mock<IMessagePollerFactory>();
        messagePollerFactoryMock.Setup(x => x.CreateMessagePoller(It.IsAny<IMessagePollerConfiguration>()))
                                .Returns(new Mock<IMessagePoller>().Object);

        var messagePumpService = new MessagePumpService(new NullLogger<MessagePumpService>(), configuration, messagePollerFactoryMock.Object);

        var combinedTask = messagePumpService.StartAsync(new CancellationToken());

        messagePollerFactoryMock.Verify(x => x.CreateMessagePoller(It.IsAny<IMessagePollerConfiguration>()), Times.Exactly(2));
        messagePollerFactoryMock.Verify(x => x.CreateMessagePoller(queueA), Times.Once());
        messagePollerFactoryMock.Verify(x => x.CreateMessagePoller(queueB), Times.Once());
    }
}
