// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.Configuration;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class MessageProcessStatusTests
{
    [Fact]
    public void IsSuccess()
    {
        var status = MessageProcessStatus.Success();
        Assert.True(status.IsSuccess);
        Assert.False(status.IsFailed);
    }

    [Fact]
    public void IsFailed()
    {
        var status = MessageProcessStatus.Failed();
        Assert.False(status.IsSuccess);
        Assert.True(status.IsFailed);
    }
}
