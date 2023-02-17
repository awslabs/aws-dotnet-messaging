// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AWS.Messaging.UnitTests.SerializationTests;

public class MessageSerializerTests
{
    [Fact]
    public void Serialize()
    {
        // ARRANGE
        IMessageSerializer serializer = new MessageSerializer(new NullLogger<MessageSerializer>(), new MessageConfiguration());
        var person = new PersonInfo
        {
            FirstName= "Bob",
            LastName = "Stone",
            Age= 30,
            Gender = Gender.Male,
            Address= new AddressInfo
            {
                Unit = 12,
                Street = "Prince St",
                ZipCode = "00001"
            }
        };

        // ACT
        var jsonString = serializer.Serialize(person);

        // ASSERT
        var expectedString = "{\"FirstName\":\"Bob\",\"LastName\":\"Stone\",\"Age\":30,\"Gender\":\"Male\",\"Address\":{\"Unit\":12,\"Street\":\"Prince St\",\"ZipCode\":\"00001\"}}";
        Assert.Equal(expectedString, jsonString);
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
                      ""ZipCode"":""00001""
                   }
                }";

        // ACT
        var message = serializer.Deserialize<PersonInfo>(jsonString);

        // ASSERT
        Assert.Equal("Bob", message.FirstName);
        Assert.Equal("Stone", message.LastName);
        Assert.Equal(30, message.Age);
        Assert.Equal(Gender.Male, message.Gender);
        Assert.Equal(12, message.Address?.Unit);
        Assert.Equal("Prince St", message.Address?.Street);
        Assert.Equal("00001", message.Address?.ZipCode);
    }
}
