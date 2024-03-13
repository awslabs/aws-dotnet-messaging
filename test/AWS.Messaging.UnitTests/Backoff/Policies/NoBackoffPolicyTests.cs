// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using AWS.Messaging.Configuration;
using AWS.Messaging.Services.Backoff.Policies;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests.Backoff.Policies;

public class NoBackoffPolicyTests
{
    [Fact]
    public void ShouldBackoffTest()
    {
        var noBackoffPolicy = new NoBackoffPolicy();

        Assert.False(noBackoffPolicy.ShouldBackoff(It.IsAny<Exception>(), It.IsAny<SQSMessagePollerConfiguration>()));
    }

    [Fact]
    public void RetrieveBackoffTimeTest()
    {
        var noBackoffPolicy = new NoBackoffPolicy();

        Assert.Equal(0, noBackoffPolicy.RetrieveBackoffTime(It.IsAny<int>()));
    }
}
