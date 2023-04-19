// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class MessageConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("CustomId")]
    public void MessageConfiguration_GetPublisherMapping(string? messageIdentifier)
    {
        var messageConfiguration = new MessageConfiguration();
        var sqsConfiguration = new SQSPublisherConfiguration("sqsQueueUrl");
        messageConfiguration.PublisherMappings.Add(new PublisherMapping(typeof(OrderInfo), sqsConfiguration, "SQS", messageIdentifier));

        Assert.Single(messageConfiguration.PublisherMappings);
        var publisherMapping = messageConfiguration.GetPublisherMapping(typeof(OrderInfo));
        Assert.NotNull(publisherMapping);
        Assert.NotNull(publisherMapping.PublisherConfiguration);
        Assert.Equal("sqsQueueUrl", publisherMapping.PublisherConfiguration.PublisherEndpoint);
    }
}
