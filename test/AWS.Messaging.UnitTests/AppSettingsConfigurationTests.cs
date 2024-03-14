// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using AWS.Messaging.Configuration;
using AWS.Messaging.Configuration.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class AppSettingsConfigurationTests
{
    private readonly IServiceCollection _serviceCollection;

    public AppSettingsConfigurationTests()
    {
        _serviceCollection = new ServiceCollection();
    }

    [Fact]
    public void AddSQSPublisher_NoIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""SQSPublishers"": [
                        {
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage"",
                            ""QueueUrl"": ""https://sqs.us-west-2.amazonaws.com/012345678910/MPF""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.PublisherMappings);
        Assert.NotEmpty(messageConfiguration.PublisherMappings);
        var publisherMapping = Assert.Single(messageConfiguration.PublisherMappings);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", publisherMapping.MessageType.FullName);
        Assert.Equal(PublisherTargetType.SQS_PUBLISHER, publisherMapping.PublishTargetType);
        Assert.Equal("https://sqs.us-west-2.amazonaws.com/012345678910/MPF", publisherMapping.PublisherConfiguration.PublisherEndpoint);
    }

    [Fact]
    public void AddSQSPublisher_WithIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""SQSPublishers"": [
                        {
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage"",
                            ""QueueUrl"": ""https://sqs.us-west-2.amazonaws.com/012345678910/MPF"",
                            ""MessageTypeIdentifier"": ""chatmessage""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.PublisherMappings);
        Assert.NotEmpty(messageConfiguration.PublisherMappings);
        var publisherMapping = Assert.Single(messageConfiguration.PublisherMappings);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", publisherMapping.MessageType.FullName);
        Assert.Equal(PublisherTargetType.SQS_PUBLISHER, publisherMapping.PublishTargetType);
        Assert.Equal("https://sqs.us-west-2.amazonaws.com/012345678910/MPF", publisherMapping.PublisherConfiguration.PublisherEndpoint);
        Assert.Equal("chatmessage", publisherMapping.MessageTypeIdentifier);
    }

    [Fact]
    public void AddSNSPublisher_NoIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""SNSPublishers"": [
                        {
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage"",
                            ""TopicUrl"": ""arn:aws:sns:us-west-2:012345678910:MPF""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.PublisherMappings);
        Assert.NotEmpty(messageConfiguration.PublisherMappings);
        var publisherMapping = Assert.Single(messageConfiguration.PublisherMappings);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", publisherMapping.MessageType.FullName);
        Assert.Equal(PublisherTargetType.SNS_PUBLISHER, publisherMapping.PublishTargetType);
        Assert.Equal("arn:aws:sns:us-west-2:012345678910:MPF", publisherMapping.PublisherConfiguration.PublisherEndpoint);
    }

    [Fact]
    public void AddSNSPublisher_WithIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""SNSPublishers"": [
                        {
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage"",
                            ""TopicUrl"": ""arn:aws:sns:us-west-2:012345678910:MPF"",
                            ""MessageTypeIdentifier"": ""chatmessage""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.PublisherMappings);
        Assert.NotEmpty(messageConfiguration.PublisherMappings);
        var publisherMapping = Assert.Single(messageConfiguration.PublisherMappings);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", publisherMapping.MessageType.FullName);
        Assert.Equal(PublisherTargetType.SNS_PUBLISHER, publisherMapping.PublishTargetType);
        Assert.Equal("arn:aws:sns:us-west-2:012345678910:MPF", publisherMapping.PublisherConfiguration.PublisherEndpoint);
        Assert.Equal("chatmessage", publisherMapping.MessageTypeIdentifier);
    }

    [Fact]
    public void AddEventBridgePublisher_NoIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""EventBridgePublishers"": [
                        {
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage"",
                            ""EventBusName"": ""arn:aws:events:us-west-2:012345678910:event-bus/default""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.PublisherMappings);
        Assert.NotEmpty(messageConfiguration.PublisherMappings);
        var publisherMapping = Assert.Single(messageConfiguration.PublisherMappings);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", publisherMapping.MessageType.FullName);
        Assert.Equal(PublisherTargetType.EVENTBRIDGE_PUBLISHER, publisherMapping.PublishTargetType);
        Assert.Equal("arn:aws:events:us-west-2:012345678910:event-bus/default", publisherMapping.PublisherConfiguration.PublisherEndpoint);
    }

    [Fact]
    public void AddEventBridgePublisher_WithIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""EventBridgePublishers"": [
                        {
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage"",
                            ""EventBusName"": ""arn:aws:events:us-west-2:012345678910:event-bus/default"",
                            ""MessageTypeIdentifier"": ""chatmessage""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.PublisherMappings);
        Assert.NotEmpty(messageConfiguration.PublisherMappings);
        var publisherMapping = Assert.Single(messageConfiguration.PublisherMappings);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", publisherMapping.MessageType.FullName);
        Assert.Equal(PublisherTargetType.EVENTBRIDGE_PUBLISHER, publisherMapping.PublishTargetType);
        Assert.Equal("arn:aws:events:us-west-2:012345678910:event-bus/default", publisherMapping.PublisherConfiguration.PublisherEndpoint);
        Assert.Equal("chatmessage", publisherMapping.MessageTypeIdentifier);
    }

    [Fact]
    public void AddMessageHandler_NoIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""MessageHandlers"": [
                        {
                            ""HandlerType"": ""AWS.Messaging.UnitTests.MessageHandlers.ChatMessageHandler"",
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.SubscriberMappings);
        Assert.NotEmpty(messageConfiguration.SubscriberMappings);
        var subscriberMapping = Assert.Single(messageConfiguration.SubscriberMappings);
        Assert.Equal("AWS.Messaging.UnitTests.MessageHandlers.ChatMessageHandler", subscriberMapping.HandlerType.FullName);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", subscriberMapping.MessageType.FullName);
    }

    [Fact]
    public void AddMessageHandler_WithIdentifier()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""MessageHandlers"": [
                        {
                            ""HandlerType"": ""AWS.Messaging.UnitTests.MessageHandlers.ChatMessageHandler"",
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage"",
                            ""MessageTypeIdentifier"": ""chatmessage""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.SubscriberMappings);
        Assert.NotEmpty(messageConfiguration.SubscriberMappings);
        var subscriberMapping = Assert.Single(messageConfiguration.SubscriberMappings);
        Assert.Equal("AWS.Messaging.UnitTests.MessageHandlers.ChatMessageHandler", subscriberMapping.HandlerType.FullName);
        Assert.Equal("AWS.Messaging.UnitTests.Models.ChatMessage", subscriberMapping.MessageType.FullName);
        Assert.Equal("chatmessage", subscriberMapping.MessageTypeIdentifier);
    }

    [Fact]
    public void AddMessageHandler_MissingRequiredField()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""MessageHandlers"": [
                        {
                            ""MessageType"": ""AWS.Messaging.UnitTests.Models.ChatMessage""
                        }
                    ]
                }
            }";

        Assert.ThrowsAny<Exception>(() =>
        {
            SetupConfigurationAndServices(json);
        });
    }

    [Fact]
    public void AddSQSPoller()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""SQSPollers"": [
                        {
                            ""QueueUrl"": ""https://sqs.us-west-2.amazonaws.com/012345678910/MPF""
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.MessagePollerConfigurations);
        Assert.NotEmpty(messageConfiguration.MessagePollerConfigurations);
        var poller = Assert.Single(messageConfiguration.MessagePollerConfigurations);
        Assert.Equal("https://sqs.us-west-2.amazonaws.com/012345678910/MPF", poller.SubscriberEndpoint);
    }

    [Fact]
    public void AddSQSPoller_WithOptions()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""SQSPollers"": [
                        {
                            ""QueueUrl"": ""https://sqs.us-west-2.amazonaws.com/012345678910/MPF"",
                            ""Options"": {
                                ""MaxNumberOfConcurrentMessages"": 10,
                                ""VisibilityTimeout"": 20,
                                ""WaitTimeSeconds"": 20,
                                ""VisibilityTimeoutExtensionHeartbeatInterval"": 1,
                                ""VisibilityTimeoutExtensionThreshold"": 5
                            }
                        }
                    ]
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.NotNull(messageConfiguration.MessagePollerConfigurations);
        Assert.NotEmpty(messageConfiguration.MessagePollerConfigurations);
        var poller = Assert.Single(messageConfiguration.MessagePollerConfigurations);
        Assert.Equal("https://sqs.us-west-2.amazonaws.com/012345678910/MPF", poller.SubscriberEndpoint);
        var sqsPoller = Assert.IsType<SQSMessagePollerConfiguration>(poller);
        Assert.Equal(10, sqsPoller.MaxNumberOfConcurrentMessages);
        Assert.Equal(20, sqsPoller.VisibilityTimeout);
        Assert.Equal(20, sqsPoller.WaitTimeSeconds);
        Assert.Equal(1, sqsPoller.VisibilityTimeoutExtensionHeartbeatInterval);
        Assert.Equal(5, sqsPoller.VisibilityTimeoutExtensionThreshold);
    }

    [Fact]
    public void AddSQSPoller_MissingRequiredField()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""SQSPollers"": [
                        {
                            ""Options"": {
                                ""MaxNumberOfConcurrentMessages"": 10,
                                ""VisibilityTimeout"": 20,
                                ""WaitTimeSeconds"": 20,
                                ""VisibilityTimeoutExtensionInterval"": 18
                            }
                        }
                    ]
                }
            }";

        Assert.ThrowsAny<Exception>(() =>
        {
            SetupConfigurationAndServices(json);
        });
    }

    [Theory]
    [InlineData("None", BackoffPolicy.None)]
    [InlineData("Interval", BackoffPolicy.Interval)]
    [InlineData("CappedExponential", BackoffPolicy.CappedExponential)]
    public void AddBackoffPolicy_NoOptions(string policyString, BackoffPolicy policyEnum)
    {
        var json = $@"
            {{
                ""AWS.Messaging"": {{
                    ""BackoffPolicy"": ""{policyString}""
                }}
            }}";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.Equal(policyEnum, messageConfiguration.BackoffPolicy);
    }

    [Fact]
    public void AddBackoffPolicy_IntervalExponentialPolicyOptions()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""BackoffPolicy"": ""Interval"",
                    ""IntervalBackoffOptions"": {
                        ""FixedInterval"": 2
                    }
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.Equal(BackoffPolicy.Interval, messageConfiguration.BackoffPolicy);
        Assert.NotNull(messageConfiguration.IntervalBackoffOptions);
        Assert.Equal(2, messageConfiguration.IntervalBackoffOptions.FixedInterval);
    }

    [Fact]
    public void AddBackoffPolicy_CappedExponentialPolicyOptions()
    {
        var json = @"
            {
                ""AWS.Messaging"": {
                    ""BackoffPolicy"": ""CappedExponential"",
                    ""CappedExponentialBackoffOptions"": {
                        ""CapBackoffTime"": 2
                    }
                }
            }";

        var messageConfiguration = SetupConfigurationAndServices(json);

        Assert.Equal(BackoffPolicy.CappedExponential, messageConfiguration.BackoffPolicy);
        Assert.NotNull(messageConfiguration.CappedExponentialBackoffOptions);
        Assert.Equal(2, messageConfiguration.CappedExponentialBackoffOptions.CapBackoffTime);
    }

    private IMessageConfiguration SetupConfigurationAndServices(string json)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(json)))
            .Build();

        _serviceCollection.AddAWSMessageBus(bus =>
        {
            bus.LoadConfigurationFromSettings(configuration);
        });

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IMessageConfiguration>();
    }
}
