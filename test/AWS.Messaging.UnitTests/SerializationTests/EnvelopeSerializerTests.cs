// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using AWS.Messaging.Serialization;
using AWS.Messaging.Services;
using AWS.Messaging.UnitTests.MessageHandlers;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace AWS.Messaging.UnitTests.SerializationTests;

public class EnvelopeSerializerTests
{
    private readonly IServiceCollection _serviceCollection;
    private readonly DateTimeOffset _testdate = new DateTime(year: 2000, month: 12, day: 5, hour: 10, minute: 30, second: 55, DateTimeKind.Utc);

    public EnvelopeSerializerTests()
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddLogging();
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<AddressInfo>("sqsQueueUrl", "addressInfo");
            builder.AddMessageHandler<AddressInfoHandler, AddressInfo>("addressInfo");
        });

        var mockDateTimeHandler = new Mock<IDateTimeHandler>();
        mockDateTimeHandler.Setup(x => x.GetUtcNow()).Returns(_testdate);
        _serviceCollection.Replace(new ServiceDescriptor(typeof(IDateTimeHandler), mockDateTimeHandler.Object));
    }

    [Fact]
    public async Task CreateEnvelope()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();
        var message = new AddressInfo
        {
            Street = "Prince St",
            Unit = 123,
            ZipCode = "00001"
        };

        // ACT
        var envelope = await envelopeSerializer.CreateEnvelopeAsync(message);

        // ASSERT
        Assert.NotNull(envelope);
        Assert.Equal(_testdate, envelope.TimeStamp);
        Assert.Equal("1.0", envelope.Version);
        Assert.Equal("/aws/messaging", envelope.Source?.ToString());
        Assert.Equal("addressInfo", envelope.MessageTypeIdentifier);

        var addressInfo = envelope.Message;
        Assert.Equal("Prince St", addressInfo?.Street);
        Assert.Equal(123, addressInfo?.Unit);
        Assert.Equal("00001", addressInfo?.ZipCode);
    }

    [Fact]
    public void SerializeEnvelope()
    {
        // ARRANGE
        var message = new AddressInfo
        {
            Street = "Prince St",
            Unit = 123,
            ZipCode = "00001"
        };

        var envelope = new MessageEnvelope<AddressInfo>
        {
            Id =  "id-123",
            Source = new Uri("/backend/service", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = message
        };

        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();

        // ACT
        var jsonBlob = envelopeSerializer.Serialize(envelope);

        // ASSERT
        // The \u0022 corresponds to quotation mark (")
        var expectedBlob = "{\"id\":\"id-123\",\"source\":\"/backend/service\",\"specversion\":\"1.0\",\"type\":\"addressInfo\",\"time\":\"2000-12-05T10:30:55+00:00\",\"data\":\"{\\u0022Unit\\u0022:123,\\u0022Street\\u0022:\\u0022Prince St\\u0022,\\u0022ZipCode\\u0022:\\u002200001\\u0022}\"}";
        Assert.Equal(expectedBlob, jsonBlob);
    }

    [Fact]
    public async Task ConvertToEnvelope()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();
        var message = new AddressInfo
        {
            Street = "Prince St",
            Unit = 123,
            ZipCode = "00001"
        };

        var envelope = await envelopeSerializer.CreateEnvelopeAsync(message);
        var sqsMessage = new Message
        {
            Body = envelopeSerializer.Serialize(envelope)
        };

        // ACT
        var result = envelopeSerializer.ConvertToEnvelope(sqsMessage);

        // ASSERT
        envelope = (MessageEnvelope<AddressInfo>)result.Envelope;
        Assert.NotNull(envelope);
        Assert.Equal(_testdate, envelope.TimeStamp);
        Assert.Equal("1.0", envelope.Version);
        Assert.Equal("/aws/messaging", envelope.Source?.ToString());
        Assert.Equal("addressInfo", envelope.MessageTypeIdentifier);

        var addressInfo = envelope.Message;
        Assert.Equal("Prince St", addressInfo?.Street);
        Assert.Equal(123, addressInfo?.Unit);
        Assert.Equal("00001", addressInfo?.ZipCode);

        var subscribeMapping = result.Mapping;
        Assert.NotNull(subscribeMapping);
        Assert.Equal("addressInfo", subscribeMapping.MessageTypeIdentifier);
        Assert.Equal(typeof(AddressInfo), subscribeMapping.MessageType);
        Assert.Equal(typeof(AddressInfoHandler), subscribeMapping.HandlerType);
    }
}
