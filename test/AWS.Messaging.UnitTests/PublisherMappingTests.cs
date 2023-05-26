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
        var sqsConfiguration = new SQSPublisherConfiguration("sqsQueueUrl");
        var mapping = new PublisherMapping(typeof(OrderInfo), sqsConfiguration, "SQS");
        Assert.Equal("AWS.Messaging.UnitTests.PublisherMappingTests+OrderInfo", mapping.MessageTypeIdentifier);
    }

    [Fact]
    public void PublisherMappingWithMessageIdentifier()
    {
        var sqsConfiguration = new SQSPublisherConfiguration("sqsQueueUrl");
        var mapping = new PublisherMapping(typeof(OrderInfo), sqsConfiguration, "SQS", "CustomIdentifier");
        Assert.Equal("CustomIdentifier", mapping.MessageTypeIdentifier);
    }

    [Fact]
    public void SqsPublisherMappingType()
    {
        var publisherConfiguration = new SQSPublisherConfiguration("sqsQueueUrl");

        var mapping = new PublisherMapping(typeof(OrderInfo), publisherConfiguration, "SQS");

        Assert.Equal("SQS", mapping.PublishTargetType);
        Assert.NotNull(mapping.PublisherConfiguration);
        Assert.Equal("sqsQueueUrl", mapping.PublisherConfiguration.PublisherEndpoint);
    }

    [Fact]
    public void SnsPublisherMappingType()
    {
        var publisherConfiguration = new SNSPublisherConfiguration("snsTopicUrl");

        var mapping = new PublisherMapping(typeof(OrderInfo), publisherConfiguration, "SNS");

        Assert.Equal("SNS", mapping.PublishTargetType);
        Assert.NotNull(mapping.PublisherConfiguration);
        Assert.Equal("snsTopicUrl", mapping.PublisherConfiguration.PublisherEndpoint);
    }

    [Fact]
    public void EventBridgePublisherMappingType()
    {
        var publisherConfiguration = new SNSPublisherConfiguration("eventBridgeUrl");

        var mapping = new PublisherMapping(typeof(OrderInfo), publisherConfiguration, "EventBridge");

        Assert.Equal("EventBridge", mapping.PublishTargetType);
        Assert.NotNull(mapping.PublisherConfiguration);
        Assert.Equal("eventBridgeUrl", mapping.PublisherConfiguration.PublisherEndpoint);
    }

    public class OrderInfo
    {
        public int Id { get; set; }
    }
}
