// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Reflection;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services.Backoff.Policies;
using AWS.Messaging.Services.Backoff.Policies.Options;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests.Backoff.Policies;

public class IntervalBackoffPolicyTests
{
    [Theory]
    [InlineData(typeof(QueueDoesNotExistException), false)]
    [InlineData(typeof(UnsupportedOperationException), false)]
    [InlineData(typeof(InvalidAddressException), false)]
    [InlineData(typeof(InvalidSecurityException), false)]
    [InlineData(typeof(KmsAccessDeniedException), false)]
    [InlineData(typeof(KmsInvalidKeyUsageException), false)]
    [InlineData(typeof(KmsInvalidStateException), false)]
    [InlineData(typeof(AmazonSQSException), true)]
    [InlineData(typeof(Exception), true)]
    public void ShouldBackoffTest(Type exceptionType, bool shouldBackoff)
    {
        var exception = Activator.CreateInstance(exceptionType, "Exception message");
        if (exception is null)
            Assert.Fail();
        var sqsMessagePollerConfiguration = new SQSMessagePollerConfiguration("queueURL");

        var intervalBackoffOptions = new IntervalBackoffOptions();
        var intervalBackoffPolicy = new IntervalBackoffPolicy(intervalBackoffOptions);

        Assert.Equal(shouldBackoff, intervalBackoffPolicy.ShouldBackoff((Exception) exception, sqsMessagePollerConfiguration));
    }

    [Fact]
    public void RetrieveBackoffTimeTest()
    {
        var intervalBackoffOptions = new IntervalBackoffOptions();
        var intervalBackoffPolicy = new IntervalBackoffPolicy(intervalBackoffOptions);

        Assert.True(TimeSpan.FromSeconds(intervalBackoffOptions.FixedInterval) >= intervalBackoffPolicy.RetrieveBackoffTime(It.IsAny<int>()));
    }
}
