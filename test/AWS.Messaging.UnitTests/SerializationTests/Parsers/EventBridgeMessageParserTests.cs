// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text.Json;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization.Parsers;
using Xunit;

namespace AWS.Messaging.UnitTests.SerializationTests.Parsers;

public class EventBridgeMessageParserTests
{
    private readonly EventBridgeMessageParser _parser;

    public EventBridgeMessageParserTests()
    {
        _parser = new EventBridgeMessageParser();
    }

    [Fact]
    public void CanParse_WithValidEventBridgeMessage_ReturnsTrue()
    {
        // Arrange
        var json = @"{
            ""detail"": { ""someData"": ""value"" },
            ""detail-type"": ""test-type"",
            ""source"": ""test-source"",
            ""time"": ""2024-03-15T10:00:00Z""
        }";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = _parser.CanParse(root);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(InvalidEventBridgeMessages))]
    public void CanParse_WithInvalidEventBridgeMessage_ReturnsFalse(string json)
    {
        // Arrange
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = _parser.CanParse(root);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WithObjectDetail_ReturnsRawJson()
    {
        // Arrange
        var json = @"{
            ""detail"": { ""someData"": ""value"" },
            ""detail-type"": ""test-type"",
            ""source"": ""test-source"",
            ""time"": ""2024-03-15T10:00:00Z"",
            ""id"": ""test-id"",
            ""account"": ""123456789012"",
            ""region"": ""us-east-1"",
            ""resources"": [""resource1"", ""resource2""]
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act
        var (messageBody, metadata) = _parser.Parse(root, message);

        // Assert
        Assert.Contains("\"someData\"", messageBody);
        Assert.Contains("\"value\"", messageBody);
        Assert.NotNull(metadata.EventBridgeMetadata);
        Assert.Equal("test-type", metadata.EventBridgeMetadata.DetailType);
        Assert.Equal("test-source", metadata.EventBridgeMetadata.Source);
        Assert.Equal("test-id", metadata.EventBridgeMetadata.EventId);
        Assert.Equal(2, metadata.EventBridgeMetadata.Resources?.Count);
    }

    [Fact]
    public void Parse_WithStringDetail_ReturnsString()
    {
        // Arrange
        var json = @"{
            ""detail"": ""string message"",
            ""detail-type"": ""test-type"",
            ""source"": ""test-source"",
            ""time"": ""2024-03-15T10:00:00Z"",
            ""id"": ""test-id""
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act
        var (messageBody, metadata) = _parser.Parse(root, message);

        // Assert
        Assert.Equal("string message", messageBody);
        Assert.NotNull(metadata.EventBridgeMetadata);
        Assert.Equal("test-type", metadata.EventBridgeMetadata.DetailType);
    }

    [Fact]
    public void Parse_WithEmptyDetail_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = @"{
            ""detail"": """",
            ""detail-type"": ""test-type"",
            ""source"": ""test-source"",
            ""time"": ""2024-03-15T10:00:00Z""
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _parser.Parse(root, message));
        Assert.Equal("EventBridge message does not contain a valid detail property", exception.Message);
    }

    [Fact]
    public void Parse_WithNullDetail_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = @"{
            ""detail"": null,
            ""detail-type"": ""test-type"",
            ""source"": ""test-source"",
            ""time"": ""2024-03-15T10:00:00Z""
        }";
        var root = JsonDocument.Parse(json).RootElement;
        var message = new Message();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _parser.Parse(root, message));
        Assert.Equal("EventBridge message does not contain a valid detail property", exception.Message);
    }

    public static TheoryData<string> InvalidEventBridgeMessages => new()
    {
        // Missing detail
        @"{
            ""detail-type"": ""test-type"",
            ""source"": ""test-source"",
            ""time"": ""2024-03-15T10:00:00Z""
        }",
        // Missing detail-type
        @"{
            ""detail"": { ""someData"": ""value"" },
            ""source"": ""test-source"",
            ""time"": ""2024-03-15T10:00:00Z""
        }",
        // Missing source
        @"{
            ""detail"": { ""someData"": ""value"" },
            ""detail-type"": ""test-type"",
            ""time"": ""2024-03-15T10:00:00Z""
        }",
        // Missing time
        @"{
            ""detail"": { ""someData"": ""value"" },
            ""detail-type"": ""test-type"",
            ""source"": ""test-source""
        }",
        // Empty object
        @"{}"
    };
}
