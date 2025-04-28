// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.Json;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization.Parsers;
using Xunit;

namespace AWS.Messaging.UnitTests.SerializationTests.Parsers;

public class SQSMessageParserTests
{
    private readonly SQSMessageParser _parser;

    public SQSMessageParserTests()
    {
        _parser = new SQSMessageParser();
    }

    [Fact]
    public void CanParse_AlwaysReturnsTrue()
    {
        // Arrange
        var validJson = @"{""key"": ""value""}";
        var emptyJson = "{}";
        var arrayJson = @"[1,2,3]";
        var validRoot = JsonDocument.Parse(validJson).RootElement;
        var emptyRoot = JsonDocument.Parse(emptyJson).RootElement;
        var arrayRoot = JsonDocument.Parse(arrayJson).RootElement;

        // Act & Assert
        Assert.True(_parser.CanParse(validRoot));
        Assert.True(_parser.CanParse(emptyRoot));
        Assert.True(_parser.CanParse(arrayRoot));
    }

    [Fact]
    public void Parse_WithSimpleMessage_ReturnsOriginalMessageAndMetadata()
    {
        // Arrange
        var json = @"{""key"": ""value""}";
        var root = JsonDocument.Parse(json).RootElement;
        var originalMessage = new Message
        {
            MessageId = "test-message-id",
            ReceiptHandle = "test-receipt-handle"
        };

        // Act
        var (messageBody, metadata) = _parser.Parse(root, originalMessage);

        // Assert
        Assert.Equal(json, messageBody);
        Assert.NotNull(metadata.SQSMetadata);
        Assert.Equal("test-message-id", metadata.SQSMetadata.MessageID);
        Assert.Equal("test-receipt-handle", metadata.SQSMetadata.ReceiptHandle);
    }

    [Fact]
    public void Parse_WithMessageAttributes_IncludesAttributesInMetadata()
    {
        // Arrange
        var json = @"{""content"": ""test""}";
        var root = JsonDocument.Parse(json).RootElement;
        var originalMessage = new Message
        {
            MessageId = "test-message-id",
            MessageAttributes = new Dictionary<string, Amazon.SQS.Model.MessageAttributeValue>
            {
                {
                    "TestAttribute",
                    new Amazon.SQS.Model.MessageAttributeValue { StringValue = "TestValue" }
                }
            }
        };

        // Act
        var (messageBody, metadata) = _parser.Parse(root, originalMessage);

        // Assert
        Assert.Equal(json, messageBody);
        Assert.NotNull(metadata.SQSMetadata);
        Assert.NotNull(metadata.SQSMetadata.MessageAttributes);
        Assert.Single(metadata.SQSMetadata.MessageAttributes);
        Assert.Equal("TestValue", metadata.SQSMetadata.MessageAttributes["TestAttribute"].StringValue);
    }

    [Fact]
    public void Parse_WithFIFOQueueAttributes_IncludesAttributesInMetadata()
    {
        // Arrange
        var json = @"{""data"": ""test""}";
        var root = JsonDocument.Parse(json).RootElement;
        var originalMessage = new Message
        {
            MessageId = "test-message-id",
            Attributes = new Dictionary<string, string>
            {
                { "MessageGroupId", "group-1" },
                { "MessageDeduplicationId", "dedup-1" }
            }
        };

        // Act
        var (messageBody, metadata) = _parser.Parse(root, originalMessage);

        // Assert
        Assert.Equal(json, messageBody);
        Assert.NotNull(metadata.SQSMetadata);
        Assert.Equal("group-1", metadata.SQSMetadata.MessageGroupId);
        Assert.Equal("dedup-1", metadata.SQSMetadata.MessageDeduplicationId);
    }

    [Fact]
    public void Parse_WithArrayMessage_ReturnsOriginalArray()
    {
        // Arrange
        var json = @"[1,2,3]";
        var root = JsonDocument.Parse(json).RootElement;
        var originalMessage = new Message
        {
            MessageId = "test-message-id"
        };

        // Act
        var (messageBody, metadata) = _parser.Parse(root, originalMessage);

        // Assert
        Assert.Equal(json, messageBody);
        Assert.NotNull(metadata.SQSMetadata);
    }

    [Fact]
    public void Parse_WithEmptyMessage_ReturnsEmptyObject()
    {
        // Arrange
        var json = "{}";
        var root = JsonDocument.Parse(json).RootElement;
        var originalMessage = new Message
        {
            MessageId = "test-message-id"
        };

        // Act
        var (messageBody, metadata) = _parser.Parse(root, originalMessage);

        // Assert
        Assert.Equal(json, messageBody);
        Assert.NotNull(metadata.SQSMetadata);
    }

    [Fact]
    public void Parse_WithComplexNestedMessage_PreservesStructure()
    {
        // Arrange
        var json = @"{
            ""id"": 1,
            ""nested"": {
                ""array"": [1,2,3],
                ""object"": {
                    ""key"": ""value""
                }
            }
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var originalMessage = new Message
        {
            MessageId = "test-message-id"
        };

        // Act
        var (messageBody, _) = _parser.Parse(root, originalMessage);

        // Assert
        var deserializedMessage = JsonDocument.Parse(messageBody).RootElement;
        Assert.Equal(1, deserializedMessage.GetProperty("id").GetInt32());
        Assert.True(deserializedMessage.GetProperty("nested").TryGetProperty("array", out var array));
        Assert.Equal(3, array.GetArrayLength());
        Assert.Equal("value", deserializedMessage.GetProperty("nested")
            .GetProperty("object")
            .GetProperty("key")
            .GetString());
    }
}
