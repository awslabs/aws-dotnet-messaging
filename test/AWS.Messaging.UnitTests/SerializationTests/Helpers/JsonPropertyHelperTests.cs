// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AWS.Messaging.Serialization.Helpers;
using Xunit;

namespace AWS.Messaging.UnitTests.SerializationTests.Helpers;

public class JsonPropertyHelperTests
{
    [Fact]
    public void GetPropertyValue_WithExistingProperty_ReturnsValue()
    {
        // Arrange
        var json = @"{""testProperty"": ""testValue""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonPropertyHelper.GetPropertyValue(root, "testProperty", element => element.GetString());

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void GetPropertyValue_WithMissingProperty_ReturnsDefault()
    {
        // Arrange
        var json = @"{""otherProperty"": ""value""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonPropertyHelper.GetPropertyValue(root, "testProperty", element => element.GetString());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRequiredProperty_WithExistingProperty_ReturnsValue()
    {
        // Arrange
        var json = @"{""testProperty"": ""testValue""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonPropertyHelper.GetRequiredProperty(root, "testProperty", element => element.GetString());

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void GetRequiredProperty_WithMissingProperty_ThrowsException()
    {
        // Arrange
        var json = @"{""otherProperty"": ""value""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(
            () => JsonPropertyHelper.GetRequiredProperty(root, "testProperty", element => element.GetString()));
        Assert.Equal("Required property 'testProperty' is missing", exception.Message);
    }

    [Fact]
    public void GetRequiredProperty_WithInvalidConversion_ThrowsException()
    {
        // Arrange
        var json = @"{""testProperty"": ""not-a-number""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act & Assert
        var exception = Assert.Throws<InvalidDataException>(
            () => JsonPropertyHelper.GetRequiredProperty(root, "testProperty", element => element.GetInt32()));
        Assert.Equal("Failed to get or convert property 'testProperty'", exception.Message);
    }

    [Fact]
    public void GetStringProperty_WithValidString_ReturnsString()
    {
        // Arrange
        var json = @"{""testProperty"": ""testValue""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonPropertyHelper.GetStringProperty(root, "testProperty");

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void GetStringProperty_WithMissingProperty_ReturnsNull()
    {
        // Arrange
        var json = @"{""otherProperty"": ""value""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act
        var result = JsonPropertyHelper.GetStringProperty(root, "testProperty");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDateTimeOffsetProperty_WithValidDate_ReturnsDateTimeOffset()
    {
        // Arrange
        var json = @"{""testDate"": ""2024-03-15T10:00:00Z""}";
        var root = JsonDocument.Parse(json).RootElement;
        var expectedDate = DateTimeOffset.Parse("2024-03-15T10:00:00Z");

        // Act
        var result = JsonPropertyHelper.GetDateTimeOffsetProperty(root, "testDate");

        // Assert
        Assert.Equal(expectedDate, result);
    }

    [Fact]
    public void GetDateTimeOffsetProperty_WithInvalidDate_ReturnsNull()
    {
        // Arrange
        var json = @"{""testDate"": ""invalid-date""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act & Assert
        Assert.Throws<FormatException>(
            () => JsonPropertyHelper.GetDateTimeOffsetProperty(root, "testDate"));
    }

    [Fact]
    public void GetUriProperty_WithValidUri_ReturnsUri()
    {
        // Arrange
        var json = @"{""testUri"": ""https://example.com""}";
        var root = JsonDocument.Parse(json).RootElement;
        var expectedUri = new Uri("https://example.com");

        // Act
        var result = JsonPropertyHelper.GetUriProperty(root, "testUri");

        // Assert
        Assert.Equal(expectedUri, result);
    }

    [Fact]
    public void GetAttributeValue_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var attributes = new Dictionary<string, string>
        {
            { "testKey", "testValue" }
        };

        // Act
        var result = JsonPropertyHelper.GetAttributeValue(attributes, "testKey");

        // Assert
        Assert.Equal("testValue", result);
    }

    [Fact]
    public void GetAttributeValue_WithMissingKey_ReturnsNull()
    {
        // Arrange
        var attributes = new Dictionary<string, string>
        {
            { "otherKey", "value" }
        };

        // Act
        var result = JsonPropertyHelper.GetAttributeValue(attributes, "testKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAttributeValue_WithEmptyDictionary_ReturnsNull()
    {
        // Arrange
        var attributes = new Dictionary<string, string>();

        // Act
        var result = JsonPropertyHelper.GetAttributeValue(attributes, "testKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPropertyValue_WithNullConverter_ThrowsArgumentNullException()
    {
        // Arrange
        var json = @"{""testProperty"": ""testValue""}";
        var root = JsonDocument.Parse(json).RootElement;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            JsonPropertyHelper.GetPropertyValue<string>(root, "testProperty", null!));
    }
}
