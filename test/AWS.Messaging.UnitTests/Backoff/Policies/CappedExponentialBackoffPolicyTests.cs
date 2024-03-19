// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services.Backoff.Policies;
using AWS.Messaging.Services.Backoff.Policies.Options;
using Xunit;

namespace AWS.Messaging.UnitTests.Backoff.Policies;

public class CappedExponentialBackoffPolicyTests
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

        var cappedExponentialBackoffOptions = new CappedExponentialBackoffOptions();
        var cappedExponentialBackoffPolicy = new CappedExponentialBackoffPolicy(cappedExponentialBackoffOptions);

        Assert.Equal(shouldBackoff, cappedExponentialBackoffPolicy.ShouldBackoff((Exception) exception, sqsMessagePollerConfiguration));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    [InlineData(5, 32)]
    [InlineData(6, 60)]
    [InlineData(7, 60)]
    [InlineData(8, 60)]
    [InlineData(9, 60)]
    [InlineData(10, 60)]
    public void RetrieveBackoffTimeTest(int retry, int backoffTime)
    {
        var cappedExponentialBackoffOptions = new CappedExponentialBackoffOptions();
        var cappedExponentialBackoffPolicy = new CappedExponentialBackoffPolicy(cappedExponentialBackoffOptions);

        Assert.True(TimeSpan.FromSeconds(backoffTime) >= cappedExponentialBackoffPolicy.RetrieveBackoffTime(retry));
    }
}
