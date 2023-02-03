// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class PublisherMappingTests
{
    [Fact]
    public void PublisherMappingNoMessageIdentifier()
    {
        var mapping = new PublisherMapping(typeof(OrderInfo), "sqsQueueUrl", "SQS");
        Assert.Equal("AWS.Messaging.UnitTests.PublisherMappingTests+OrderInfo", mapping.MessageTypeIdentifier);
    }

    [Fact]
    public void PublisherMappingWithMessageIdentifier()
    {
        var mapping = new PublisherMapping(typeof(OrderInfo), "sqsQueueUrl", "SQS", "CustomIdentifier");
        Assert.Equal("CustomIdentifier", mapping.MessageTypeIdentifier);
    }

    [Theory]
    [InlineData("SQS", "sqsQueueUrl")]
    [InlineData("SNS", "snsTopicUrl")]
    [InlineData("EventBridge", "eventBridgeUrl")]
    public void PublisherMappingTypes(string publisherType, string publisherEndpoint)
    {
        var mapping = new PublisherMapping(typeof(OrderInfo), publisherEndpoint, publisherType);

        Assert.Equal(publisherType, mapping.PublishTargetType);
        Assert.NotNull(mapping.PublisherConfiguration);
        Assert.Equal(publisherEndpoint, mapping.PublisherConfiguration.GetPublisherEndpoint());
    }

    public class OrderInfo
    {
        public int Id { get; set; }
    }
}
