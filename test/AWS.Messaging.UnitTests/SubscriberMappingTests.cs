// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.UnitTests.MessageHandlers;
using AWS.Messaging.UnitTests.Models;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class SubscriberMappingTests
{
    [Fact]
    public void SubscriberMappingNoMessageIdentifier()
    {
        var mapping = SubscriberMapping.Create<ChatMessageHandler, ChatMessage>();
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", mapping.MessageTypeIdentifier);
    }

    [Fact]
    public void SubscriberMappingWithMessageIdentifier()
    {
        var mapping = SubscriberMapping.Create<ChatMessageHandler, ChatMessage>("CustomIdentifier");
        Assert.Equal("CustomIdentifier", mapping.MessageTypeIdentifier);
    }
}
