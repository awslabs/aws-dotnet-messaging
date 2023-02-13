// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using Microsoft.Extensions.Logging;
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

class PersonInfo
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Age { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Gender Gender { get; set; }
    public AddressInfo? Address { get; set; }
}

class AddressInfo
{
    public int Unit { get; set; }
    public string? Street { get; set; }
    public string? ZipCode { get; set; }
}

enum Gender
{
    Male,
    Female
}
