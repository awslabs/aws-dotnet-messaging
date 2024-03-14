// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services.Backoff;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests.Backoff;

public class BackoffHandlerTests
{
    private readonly Mock<IBackoffPolicy> _backoffPolicy = new();
    private readonly Mock<ILogger<BackoffHandler>> _logger = new();

    [Fact]
    public async Task RetryAsync_NoException()
    {
        var source = new CancellationTokenSource();
        var sqsMessagePollerConfiguration = new SQSMessagePollerConfiguration("queueURL");
        var backoffHandler = new BackoffHandler(_backoffPolicy.Object, _logger.Object);

        await backoffHandler.BackoffAsync(() => Task.CompletedTask,
            sqsMessagePollerConfiguration,
            source.Token);

        _backoffPolicy.Verify(x => x.ShouldBackoff(It.IsAny<Exception>(), sqsMessagePollerConfiguration), Times.Never);
        _backoffPolicy.Verify(x => x.RetrieveBackoffTime(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RetryAsync_ShouldNotBackoff()
    {
        var source = new CancellationTokenSource();
        var sqsMessagePollerConfiguration = new SQSMessagePollerConfiguration("queueURL");
        var backoffHandler = new BackoffHandler(_backoffPolicy.Object, _logger.Object);
        _backoffPolicy
            .Setup(x =>
                x.ShouldBackoff(It.IsAny<Exception>(), sqsMessagePollerConfiguration))
            .Returns(false);

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await backoffHandler.BackoffAsync(() => throw new Exception("Failed to process."),
                sqsMessagePollerConfiguration,
                source.Token);
        });

        _backoffPolicy.Verify(x => x.ShouldBackoff(It.IsAny<Exception>(), sqsMessagePollerConfiguration), Times.Once);
        _backoffPolicy.Verify(X => X.RetrieveBackoffTime(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(1000, 2)]
    [InlineData(2000, 3)]
    [InlineData(3000, 4)]
    [InlineData(4000, 5)]
    [InlineData(5000, 6)]
    public async Task RetryAsync_IntervalBackoff(int cancelAfter, int retries)
    {
        var source = new CancellationTokenSource();
        var sqsMessagePollerConfiguration = new SQSMessagePollerConfiguration("queueURL");
        var backoffHandler = new BackoffHandler(_backoffPolicy.Object, _logger.Object);
        _backoffPolicy
            .Setup(x =>
                x.ShouldBackoff(It.IsAny<Exception>(), sqsMessagePollerConfiguration))
            .Returns(true);
        _backoffPolicy
            .Setup(x => x.RetrieveBackoffTime(It.IsAny<int>()))
            .Returns(1);

        source.CancelAfter(cancelAfter);
        try
        {
            await backoffHandler.BackoffAsync(() => throw new Exception("Failed to process."),
                sqsMessagePollerConfiguration,
                source.Token);
        }
        catch (TaskCanceledException) { }

        _backoffPolicy.Verify(X => X.RetrieveBackoffTime(It.IsAny<int>()), Times.AtMost(retries));
    }
}
