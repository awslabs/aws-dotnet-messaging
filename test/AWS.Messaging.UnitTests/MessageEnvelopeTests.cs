// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class MessageEnvelopeTests
{
    [Fact]
    public void MessageEnvelope_AdheresTo_CloudEventsSpec()
    {
        // ARRANGE
        var cloudEventJSON =
            @"{
               ""id"":""A1234"",
               ""source"":""/backend-service/order-placed"",
               ""specversion"":""1.0"",
               ""type"":""order-info"",
               ""time"":""2018-04-05T17:31:00"",
               ""data"":{
                  ""name"":""Bob"",
                  ""city"":""my-city"",
                  ""merchandise"":""t-shirt""
               },
               ""SQSMetadata"":{
                  ""MessageDeduplicationId"":""dedup-id"",
                  ""MessageGroupId"":""group-id"",
                  ""MessageAttributes"":{
                     ""MyNameAttribute"":{
                        ""StringValue"":""John Doe""
                     }
                  }
               },
               ""some-metadata"":""random-string""
            }";

        // ACT
        var messageEnvelope = JsonSerializer.Deserialize<MessageEnvelope<OrderInfo>>(cloudEventJSON);

        // ASSERT
        Assert.NotNull(messageEnvelope);
        Assert.Equal("A1234", messageEnvelope.Id);
        Assert.Equal("1.0", messageEnvelope.Version);
        Assert.Equal("order-info", messageEnvelope.MessageTypeIdentifier);
        Assert.Equal("/backend-service/order-placed", messageEnvelope.Source!.ToString());
        Assert.Equal(new DateTime(2018, 4, 5, 17, 31, 0), messageEnvelope.TimeStamp);
        Assert.Equal("Bob", messageEnvelope.Message!.Name);
        Assert.Equal("my-city", messageEnvelope.Message.City);
        Assert.Equal("t-shirt", messageEnvelope.Message.Merchandise);
        Assert.NotNull(messageEnvelope.Metadata);
        Assert.Equal("random-string", ((JsonElement)messageEnvelope.Metadata["some-metadata"]).Deserialize<string>());
        Assert.NotNull(messageEnvelope.SQSMetadata);
        Assert.Equal("dedup-id", messageEnvelope.SQSMetadata.MessageDeduplicationId);
        Assert.Equal("group-id", messageEnvelope.SQSMetadata.MessageGroupId);
        Assert.Equal("John Doe", messageEnvelope.SQSMetadata.MessageAttributes!["MyNameAttribute"].StringValue);
    }
}

public class OrderInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("merchandise")]
    public string Merchandise { get; set; } = string.Empty;
}
