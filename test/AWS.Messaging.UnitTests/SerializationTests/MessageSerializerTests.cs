// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests.SerializationTests;

public class MessageSerializerTests
{
    private readonly Mock<ILogger<MessageSerializer>> _logger;

    public MessageSerializerTests()
    {
        _logger = new Mock<ILogger<MessageSerializer>>();
    }

    [Fact]
    public void Serialize()
    {
        // ARRANGE
        IMessageSerializer serializer = new MessageSerializer(new NullLogger<MessageSerializer>(), new MessageConfiguration());
        var person = new PersonInfo
        {
            FirstName = "Bob",
            LastName = "Stone",
            Age = 30,
            Gender = Gender.Male,
            Address = new AddressInfo
            {
                Unit = 12,
                Street = "Prince St",
                ZipCode = "100010"
            }
        };

        // ACT
        var jsonNode = serializer.Serialize(person);

        // ASSERT
        var expectedNode = JsonNode.Parse("{\"FirstName\":\"Bob\",\"LastName\":\"Stone\",\"Age\":30,\"Gender\":\"Male\",\"Address\":{\"Unit\":12,\"Street\":\"Prince St\",\"ZipCode\":\"100010\"}}");
        Assert.Equal(expectedNode!.ToJsonString(), jsonNode.ToJsonString());
    }

    [Fact]
    public void Serialize_NoDataMessageLogging_NoError()
    {
        var messageConfiguration = new MessageConfiguration();
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        var person = new PersonInfo
        {
            FirstName = "Bob",
            LastName = "Stone",
            Age = 30,
            Gender = Gender.Male,
            Address = new AddressInfo
            {
                Unit = 12,
                Street = "Prince St",
                ZipCode = "100010"
            }
        };

        serializer.Serialize(person);

        _logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public void Serialize_DataMessageLogging_NoError()
    {
        var messageConfiguration = new MessageConfiguration { LogMessageContent = true };
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        var person = new PersonInfo
        {
            FirstName = "Bob",
            LastName = "Stone",
            Age = 30,
            Gender = Gender.Male,
            Address = new AddressInfo
            {
                Unit = 12,
                Street = "Prince St",
                ZipCode = "100010"
            }
        };

        serializer.Serialize(person);

        _logger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Trace),
                It.Is<EventId>(eventId => eventId.Id == 0),
                It.Is<It.IsAnyType>((@object, @type) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private class UnsupportedType
    {
        public string? Name { get; set; }
        public UnsupportedType? Type { get; set; }
    }

    [Fact]
    public void Serialize_NoDataMessageLogging_WithError()
    {
        var messageConfiguration = new MessageConfiguration();
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        // Creating an object with circular dependency to force an exception in the JsonSerializer.Serialize method.
        var unsupportedType1 = new UnsupportedType { Name = "type1" };
        var unsupportedType2 = new UnsupportedType { Name = "type2" };
        unsupportedType1.Type = unsupportedType2;
        unsupportedType2.Type = unsupportedType1;

        var exception = Assert.Throws<FailedToSerializeApplicationMessageException>(() => serializer.Serialize(unsupportedType1));

        Assert.Equal("Failed to serialize application message into a string", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Serialize_DataMessageLogging_WithError()
    {
        var messageConfiguration = new MessageConfiguration { LogMessageContent = true };
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        // Creating an object with circular dependency to force an exception in the JsonSerializer.Serialize method.
        var unsupportedType1 = new UnsupportedType { Name = "type1" };
        var unsupportedType2 = new UnsupportedType { Name = "type2" };
        unsupportedType1.Type = unsupportedType2;
        unsupportedType2.Type = unsupportedType1;

        var exception = Assert.Throws<FailedToSerializeApplicationMessageException>(() => serializer.Serialize(unsupportedType1));

        Assert.Equal("Failed to serialize application message into a string", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Deserialize()
    {
        // ARRANGE
        IMessageSerializer serializer = new MessageSerializer(new NullLogger<MessageSerializer>(), new MessageConfiguration());
        var jsonString =
            @"{
                   ""FirstName"":""Bob"",
                   ""LastName"":""Stone"",
                   ""Age"":30,
                   ""Gender"":""Male"",
                   ""Address"":{
                      ""Unit"":12,
                      ""Street"":""Prince St"",
                      ""ZipCode"":""100010""
                   }
                }";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        // ACT
        var message = (PersonInfo)serializer.Deserialize(jsonElement, typeof(PersonInfo));

        // ASSERT
        Assert.Equal("Bob", message.FirstName);
        Assert.Equal("Stone", message.LastName);
        Assert.Equal(30, message.Age);
        Assert.Equal(Gender.Male, message.Gender);
        Assert.Equal(12, message.Address?.Unit);
        Assert.Equal("Prince St", message.Address?.Street);
        Assert.Equal("100010", message.Address?.ZipCode);
    }

    [Fact]
    public void Deserialize_NoDataMessageLogging_NoError()
    {
        var messageConfiguration = new MessageConfiguration();
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        var jsonString =
            @"{
                   ""FirstName"":""Bob"",
                   ""LastName"":""Stone"",
                   ""Age"":30,
                   ""Gender"":""Male"",
                   ""Address"":{
                      ""Unit"":12,
                      ""Street"":""Prince St"",
                      ""ZipCode"":""100010""
                   }
                }";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        serializer.Deserialize(jsonElement, typeof(PersonInfo));

        _logger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Trace),
                It.Is<EventId>(eventId => eventId.Id == 0),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == "Deserializing the following message into type 'AWS.Messaging.UnitTests.Models.PersonInfo'"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Deserialize_DataMessageLogging_NoError()
    {
        var messageConfiguration = new MessageConfiguration { LogMessageContent = true };
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        var jsonString = @"{""FirstName"":""Bob""}";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        serializer.Deserialize(jsonElement, typeof(PersonInfo));

        _logger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Trace),
                It.Is<EventId>(eventId => eventId.Id == 0),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString().Contains("Deserializing the following message into type 'AWS.Messaging.UnitTests.Models.PersonInfo'")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Deserialize_NoDataMessageLogging_WithError()
    {
        var messageConfiguration = new MessageConfiguration();
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        var jsonString = @"{""Age"":""not-a-number""}";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var exception = Assert.Throws<FailedToDeserializeApplicationMessageException>(
            () => serializer.Deserialize(jsonElement, typeof(PersonInfo)));

        Assert.Equal("Failed to deserialize application message into an instance of AWS.Messaging.UnitTests.Models.PersonInfo.", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Deserialize_DataMessageLogging_WithError()
    {
        var messageConfiguration = new MessageConfiguration { LogMessageContent = true };
        IMessageSerializer serializer = new MessageSerializer(_logger.Object, messageConfiguration);

        var jsonString = @"{""Age"":""not-a-number""}";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);

        var exception = Assert.Throws<FailedToDeserializeApplicationMessageException>(
            () => serializer.Deserialize(jsonElement, typeof(PersonInfo)));

        Assert.Equal("Failed to deserialize application message into an instance of AWS.Messaging.UnitTests.Models.PersonInfo.", exception.Message);
        Assert.NotNull(exception.InnerException);
    }
}
