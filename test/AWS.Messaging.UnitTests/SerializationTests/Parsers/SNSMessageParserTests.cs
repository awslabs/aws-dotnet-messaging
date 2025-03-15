// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text.Json;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization.Parsers;
using Xunit;

namespace AWS.Messaging.UnitTests.SerializationTests.Parsers;

public class SNSMessageParserTests
{
    private readonly SNSMessageParser _parser;

    public SNSMessageParserTests()
    {
        _parser = new SNSMessageParser();
    }

    [Fact]
    public void CanParse_WithValidSNSMessage_ReturnsTrue()
    {
        // Arrange
        var json = @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""test-message-id"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic"",
            ""Message"": ""test message""
        }";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = _parser.CanParse(root);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(InvalidSNSMessages))]
    public void CanParse_WithInvalidSNSMessage_ReturnsFalse(string json)
    {
        // Arrange
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = _parser.CanParse(root);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WithValidMessage_ReturnsMessageAndMetadata()
    {
        // Arrange
        var json = @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""test-message-id"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic"",
            ""Message"": ""test message"",
            ""Timestamp"": ""2024-03-15T10:00:00.000Z"",
            ""Subject"": ""Test Subject"",
            ""UnsubscribeURL"": ""https://sns.region.amazonaws.com/unsubscribe"",
            ""MessageAttributes"": {
                ""TestAttribute"": {
                    ""Type"": ""String"",
                    ""Value"": ""TestValue""
                }
            }
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act
        var (messageBody, metadata) = _parser.Parse(root, message);

        // Assert
        Assert.Equal("test message", messageBody);
        Assert.NotNull(metadata.SNSMetadata);
        Assert.Equal("test-message-id", metadata.SNSMetadata.MessageId);
        Assert.Equal("arn:aws:sns:region:account:topic", metadata.SNSMetadata.TopicArn);
        Assert.Equal("Test Subject", metadata.SNSMetadata.Subject);
        Assert.Equal("https://sns.region.amazonaws.com/unsubscribe", metadata.SNSMetadata.UnsubscribeURL);
    }

    [Fact]
    public void Parse_WithMissingMessage_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""test-message-id"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic""
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(
            () => _parser.Parse(root, message));
    }

    [Fact]
    public void Parse_WithNullMessage_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""test-message-id"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic"",
            ""Message"": null
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _parser.Parse(root, message));
        Assert.Equal("SNS message does not contain a valid Message property", exception.Message);
    }

    [Fact]
    public void Parse_WithEmptyMessage_ReturnsEmptyString()
    {
        // Arrange
        var json = @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""test-message-id"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic"",
            ""Message"": """"
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act
        var (messageBody, metadata) = _parser.Parse(root, message);

        // Assert
        Assert.Equal("", messageBody);
        Assert.NotNull(metadata.SNSMetadata);
    }

    public static TheoryData<string> InvalidSNSMessages => new()
    {
        // Missing Type
        @"{
            ""MessageId"": ""test-message-id"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic"",
            ""Message"": ""test message""
        }",
        // Wrong Type
        @"{
            ""Type"": ""WrongType"",
            ""MessageId"": ""test-message-id"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic"",
            ""Message"": ""test message""
        }",
        // Missing MessageId
        @"{
            ""Type"": ""Notification"",
            ""TopicArn"": ""arn:aws:sns:region:account:topic"",
            ""Message"": ""test message""
        }",
        // Missing TopicArn
        @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""test-message-id"",
            ""Message"": ""test message""
        }",
        // Empty object
        @"{}"
    };

    [Fact]
    public void Parse_WithJsonObjectMessage_ReturnsJsonString()
    {
        // Arrange
        var json = @"{
        ""Type"": ""Notification"",
        ""MessageId"": ""test-message-id"",
        ""TopicArn"": ""arn:aws:sns:region:account:topic"",
        ""Message"": ""{\""key\"":\""value\""}""
    }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act
        var (messageBody, metadata) = _parser.Parse(root, message);

        // Assert
        Assert.Equal("{\"key\":\"value\"}", messageBody);
        Assert.NotNull(metadata.SNSMetadata);
    }


}
