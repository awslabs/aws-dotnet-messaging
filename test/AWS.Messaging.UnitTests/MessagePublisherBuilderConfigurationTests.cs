// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class MessagePublisherBuilderConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("CustomId")]
    public void BuilderConfiguration_GetPublisherMapping(string? messageIdentifier)
    {
        var builderConfiguration = new MessagePublisherBuilderConfiguration();
        builderConfiguration.PublisherMappings.Add(new PublisherMapping(typeof(OrderInfo), "sqsQueueUrl", "SQS", messageIdentifier));

        Assert.Single(builderConfiguration.PublisherMappings);
        var publisherMapping = builderConfiguration.GetPublisherMapping(typeof(OrderInfo));
        Assert.NotNull(publisherMapping);
        Assert.NotNull(publisherMapping.PublisherConfiguration);
        Assert.Equal("sqsQueueUrl", publisherMapping.PublisherConfiguration.GetPublisherEndpoint());
    }
}
