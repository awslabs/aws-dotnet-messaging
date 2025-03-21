// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using AWS.Messaging.Services;
using AWS.Messaging.UnitTests.MessageHandlers;
using AWS.Messaging.UnitTests.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
            builder.AddMessageSource("/aws/messaging");
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
    public async Task CreateEnvelope_MissingPublisherMapping_ThrowsException()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();

        var message = new ChatMessage
        {
            MessageDescription = "This is a test message"
        };

        // ACT and ASSERT
        // This throws an exception since no publisher is configured against the ChatMessage type.
        await Assert.ThrowsAsync<FailedToCreateMessageEnvelopeException>(async () => await envelopeSerializer.CreateEnvelopeAsync(message));
    }


    [Fact]
    public async Task SerializeEnvelope()
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
        var jsonBlob = await envelopeSerializer.SerializeAsync(envelope);

        // ASSERT
        // The \u0022 corresponds to quotation mark (")
        var expectedBlob = "{\"id\":\"id-123\",\"source\":\"/backend/service\",\"specversion\":\"1.0\",\"type\":\"addressInfo\",\"time\":\"2000-12-05T10:30:55+00:00\",\"data\":\"{\\u0022Unit\\u0022:123,\\u0022Street\\u0022:\\u0022Prince St\\u0022,\\u0022ZipCode\\u0022:\\u002200001\\u0022}\"}";
        Assert.Equal(expectedBlob, jsonBlob);
    }

    [Fact]
    public async Task ConvertToEnvelope_NoOuterEnvelope_In_SQSMessageBody()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();
        var messageEnvelope = new MessageEnvelope<AddressInfo>
        {
            Id = "66659d05-e4ff-462f-81c4-09e560e66a5c",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = new AddressInfo
            {
                Street = "Prince St",
                Unit = 123,
                ZipCode = "00001"
            }

        };
        var sqsMessage = new Message
        {
            Body = await envelopeSerializer.SerializeAsync(messageEnvelope),
            ReceiptHandle = "receipt-handle"
        };
        sqsMessage.MessageAttributes.Add("attr1", new MessageAttributeValue { DataType = "String", StringValue = "val1" });
        sqsMessage.Attributes.Add("MessageGroupId", "group-123");
        sqsMessage.Attributes.Add("MessageDeduplicationId", "dedup-123");

        // ACT
        var result = await envelopeSerializer.ConvertToEnvelopeAsync(sqsMessage);

        // ASSERT
        var envelope = (MessageEnvelope<AddressInfo>)result.Envelope;
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

        var sqsMetadata = envelope.SQSMetadata!;
        Assert.Equal("receipt-handle", sqsMetadata.ReceiptHandle);
        Assert.Equal("group-123", sqsMetadata.MessageGroupId);
        Assert.Equal("dedup-123", sqsMetadata.MessageDeduplicationId);
        Assert.Equal("String", sqsMetadata.MessageAttributes!["attr1"].DataType);
        Assert.Equal("val1", sqsMetadata.MessageAttributes["attr1"].StringValue);
    }

    [Fact]
    public async Task ConvertToEnvelope_With_SNSOuterEnvelope_In_SQSMessageBody()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();

        var innerMessageEnvelope = new MessageEnvelope<AddressInfo>
        {
            Id = "66659d05-e4ff-462f-81c4-09e560e66a5c",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = new AddressInfo
            {
                Street = "Prince St",
                Unit = 123,
                ZipCode = "00001"
            }
        };

        var outerMessageEnvelope = new Dictionary<string, object>
        {
            { "Type", "Notification" },
            { "MessageId", "abcd-123" },
            { "TopicArn", "arn:aws:sns:us-east-2:111122223333:ExampleTopic1" },
            { "Subject", "TestSubject" },
            { "Timestamp", _testdate },
            { "SignatureVersion", "1" },
            { "Signature", "abcdef33242" },
            { "SigningCertURL", "https://sns.us-east-2.amazonaws.com/SimpleNotificationService-010a507c1833636cd94bdb98bd93083a.pem" },
            { "UnsubscribeURL", "https://www.click-here.com" },
            { "Message", await envelopeSerializer.SerializeAsync(innerMessageEnvelope) },
            { "MessageAttributes", new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>
            {
                { "attr1", new Amazon.SimpleNotificationService.Model.MessageAttributeValue{ DataType = "String", StringValue = "val1"} },
                { "attr2", new Amazon.SimpleNotificationService.Model.MessageAttributeValue{ DataType = "Number", StringValue = "3"} }
            } }
        };

        var sqsMessage = new Message
        {
            Body = JsonSerializer.Serialize(outerMessageEnvelope)
        };

        // ACT
        var result = await envelopeSerializer.ConvertToEnvelopeAsync(sqsMessage);

        // ASSERT
        var envelope = (MessageEnvelope<AddressInfo>)result.Envelope;
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

        var snsMetadata = envelope.SNSMetadata!;
        Assert.Equal("arn:aws:sns:us-east-2:111122223333:ExampleTopic1", snsMetadata.TopicArn);
        Assert.Equal("https://www.click-here.com", snsMetadata.UnsubscribeURL);
        Assert.Equal("abcd-123", snsMetadata.MessageId);
        Assert.Equal("TestSubject", snsMetadata.Subject);
        Assert.Equal(_testdate, snsMetadata.Timestamp);
        Assert.Equal("String", snsMetadata.MessageAttributes!["attr1"].DataType);
        Assert.Equal("val1", snsMetadata.MessageAttributes["attr1"].StringValue);
        Assert.Equal("Number", snsMetadata.MessageAttributes["attr2"].DataType);
        Assert.Equal("3", snsMetadata.MessageAttributes["attr2"].StringValue);
    }

    [Fact]
    public async Task ConvertToEnvelope_With_EventBridgeOuterEnvelope_In_SQSMessageBody()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();

        var innerMessageEnvelope = new MessageEnvelope<string>
        {
            Id = "66659d05-e4ff-462f-81c4-09e560e66a5c",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = JsonSerializer.Serialize(new AddressInfo
            {
                Street = "Prince St",
                Unit = 123,
                ZipCode = "00001"
            })
        };

        var outerMessageEnvelope = new Dictionary<string, object>
        {
            { "version", "0" },
            { "id", "abcd-123" },
            { "source", "some-source" },
            { "detail-type", "address" },
            { "time", _testdate },
            { "account", "123456789123" },
            { "region", "us-west-2" },
            { "resources", new List<string>{ "arn1", "arn2" } },
            { "detail", innerMessageEnvelope }, // The "detail" property is set as a JSON object and not a string.
        };

        var sqsMessage = new Message
        {
            Body = JsonSerializer.Serialize(outerMessageEnvelope)
        };

        // ACT
        var result = await envelopeSerializer.ConvertToEnvelopeAsync(sqsMessage);

        // ASSERT
        var envelope = (MessageEnvelope<AddressInfo>)result.Envelope;
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

        var eventBridgeMetadata = envelope.EventBridgeMetadata!;
        Assert.Equal("abcd-123", eventBridgeMetadata.EventId);
        Assert.Equal("some-source", eventBridgeMetadata.Source);
        Assert.Equal("address", eventBridgeMetadata.DetailType);
        Assert.Equal(_testdate, eventBridgeMetadata.Time);
        Assert.Equal("123456789123", eventBridgeMetadata.AWSAccount);
        Assert.Equal("us-west-2", eventBridgeMetadata.AWSRegion);
        Assert.Equal(new List<string> { "arn1", "arn2" }, eventBridgeMetadata.Resources);
    }

    [Fact]
    public async Task ConvertToEnvelope_MissingSubscriberMapping_ThrowsException()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();
        var messageEnvelope = new MessageEnvelope<ChatMessage>
        {
            Id = "66659d05-e4ff-462f-81c4-09e560e66a5c",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "chatmessage",
            TimeStamp = _testdate,
            Message = new ChatMessage
            {
                MessageDescription = "This is a test message"
            }

        };
        var sqsMessage = new Message
        {
            Body = await envelopeSerializer.SerializeAsync(messageEnvelope),
            ReceiptHandle = "receipt-handle"
        };

        // ACT and ASSERT
        // This throws an exception because no subscriber is configured against the ChatMessage type.
        await Assert.ThrowsAsync<FailedToCreateMessageEnvelopeException>(async () => await envelopeSerializer.ConvertToEnvelopeAsync(sqsMessage));
    }

    [Fact]
    public async Task SerializationCallbacks_AreCorrectlyInvoked()
    {
        // ARRANGE
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddMessageHandler<AddressInfoHandler, AddressInfo>("addressInfo");
            builder.AddSerializationCallback(new MockSerializationCallback());
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();
        var messageEnvelope = new MessageEnvelope<AddressInfo>
        {
            Id = "123",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = new AddressInfo
            {
                Street = "Prince St",
                Unit = 123,
                ZipCode = "00001"
            }
        };

        // ACT - Serialize Envelope
        var serializedMessage = await envelopeSerializer.SerializeAsync(messageEnvelope);

        // ASSERT - Check expected base 64 encoded string
        var expectedserializedMessage = "eyJpZCI6IjEyMyIsInNvdXJjZSI6Ii9hd3MvbWVzc2FnaW5nIiwic3BlY3ZlcnNpb24iOiIxLjAiLCJ0eXBlIjoiYWRkcmVzc0luZm8iLCJ0aW1lIjoiMjAwMC0xMi0wNVQxMDozMDo1NSswMDowMCIsImRhdGEiOiJ7XHUwMDIyVW5pdFx1MDAyMjoxMjMsXHUwMDIyU3RyZWV0XHUwMDIyOlx1MDAyMlByaW5jZSBTdFx1MDAyMixcdTAwMjJaaXBDb2RlXHUwMDIyOlx1MDAyMjAwMDAxXHUwMDIyfSIsIklzLURlbGl2ZXJlZCI6ZmFsc2V9";
        Assert.Equal(expectedserializedMessage, serializedMessage);

        // ACT - Convert To Envelope from base 64 Encoded Message
        var sqsMessage = new Message
        {
            Body = serializedMessage
        };

        var conversionResult = await envelopeSerializer.ConvertToEnvelopeAsync(sqsMessage);

        // ASSERT
        var envelope = (MessageEnvelope<AddressInfo>)conversionResult.Envelope;
        Assert.NotNull(envelope);
        Assert.Equal("123", envelope.Id);
        Assert.Equal(_testdate, envelope.TimeStamp);
        Assert.Equal("1.0", envelope.Version);
        Assert.Equal("/aws/messaging", envelope.Source?.ToString());
        Assert.Equal("addressInfo", envelope.MessageTypeIdentifier);
        Assert.True(envelope.Metadata["Is-Delivered"].GetBoolean());

        var subscribeMapping = conversionResult.Mapping;
        Assert.NotNull(subscribeMapping);
        Assert.Equal("addressInfo", subscribeMapping.MessageTypeIdentifier);
        Assert.Equal(typeof(AddressInfo), subscribeMapping.MessageType);
        Assert.Equal(typeof(AddressInfoHandler), subscribeMapping.HandlerType);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SerializeAsync_DataMessageLogging_NoError(bool dataMessageLogging)
    {
        var logger = new Mock<ILogger<EnvelopeSerializer>>();
        var messageConfiguration = new MessageConfiguration { LogMessageContent = dataMessageLogging };
        var messageSerializer = new Mock<IMessageSerializer>();
        var dateTimeHandler = new Mock<IDateTimeHandler>();
        var messageIdGenerator = new Mock<IMessageIdGenerator>();
        var messageSourceHandler = new Mock<IMessageSourceHandler>();
        var envelopeSerializer = new EnvelopeSerializer(logger.Object, messageConfiguration, messageSerializer.Object, dateTimeHandler.Object, messageIdGenerator.Object, messageSourceHandler.Object);
        var messageEnvelope = new MessageEnvelope<AddressInfo>
        {
            Id = "123",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = new AddressInfo
            {
                Street = "Prince St",
                Unit = 123,
                ZipCode = "00001"
            }
        };

        await envelopeSerializer.SerializeAsync(messageEnvelope);

        if (dataMessageLogging)
        {
            logger.Verify(log => log.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Trace),
                    It.Is<EventId>(eventId => eventId.Id == 0),
                    It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == "Serialized the MessageEnvelope object as the following raw string:\n{\"id\":\"123\",\"source\":\"/aws/messaging\",\"specversion\":\"1.0\",\"type\":\"addressInfo\",\"time\":\"2000-12-05T10:30:55+00:00\",\"data\":null}"),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        else
        {
            logger.Verify(log => log.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Trace),
                    It.Is<EventId>(eventId => eventId.Id == 0),
                    It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == "Serialized the MessageEnvelope object to a raw string"),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SerializeAsync_DataMessageLogging_WithError(bool dataMessageLogging)
    {
        var logger = new Mock<ILogger<EnvelopeSerializer>>();
        var messageConfiguration = new MessageConfiguration { LogMessageContent = dataMessageLogging };
        var messageSerializer = new Mock<IMessageSerializer>();
        var dateTimeHandler = new Mock<IDateTimeHandler>();
        var messageIdGenerator = new Mock<IMessageIdGenerator>();
        var messageSourceHandler = new Mock<IMessageSourceHandler>();
        var envelopeSerializer = new EnvelopeSerializer(logger.Object, messageConfiguration, messageSerializer.Object, dateTimeHandler.Object, messageIdGenerator.Object, messageSourceHandler.Object);
        var messageEnvelope = new MessageEnvelope<MessageEnvelope>
        {
            Id = "123",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = new MessageEnvelope<MessageEnvelope>
            {
                Id = "123",
                Source = new Uri("/aws/messaging", UriKind.Relative),
                Version = "1.0",
                MessageTypeIdentifier = "addressInfo",
                TimeStamp = _testdate
            }
        };
        messageSerializer.Setup(x => x.Serialize(It.IsAny<object>())).Throws(new JsonException("Test exception"));

        var exception = await Assert.ThrowsAsync<FailedToSerializeMessageEnvelopeException>(async () => await envelopeSerializer.SerializeAsync(messageEnvelope));

        Assert.Equal("Failed to serialize the MessageEnvelope into a raw string", exception.Message);
        if (dataMessageLogging)
        {
            Assert.NotNull(exception.InnerException);
        }
        else
        {
            Assert.Null(exception.InnerException);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConvertToEnvelopeAsync_DataMessageLogging_WithError(bool dataMessageLogging)
    {
        var logger = new Mock<ILogger<EnvelopeSerializer>>();
        var messageConfiguration = new MessageConfiguration { LogMessageContent = dataMessageLogging };
        var messageSerializer = new Mock<IMessageSerializer>();
        var dateTimeHandler = new Mock<IDateTimeHandler>();
        var messageIdGenerator = new Mock<IMessageIdGenerator>();
        var messageSourceHandler = new Mock<IMessageSourceHandler>();
        var envelopeSerializer = new EnvelopeSerializer(logger.Object, messageConfiguration, messageSerializer.Object, dateTimeHandler.Object, messageIdGenerator.Object, messageSourceHandler.Object);
        var messageEnvelope = new MessageEnvelope<MessageEnvelope>
        {
            Id = "123",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "addressInfo",
            TimeStamp = _testdate,
            Message = new MessageEnvelope<string>
            {
                Id = "123",
                Source = new Uri("/aws/messaging", UriKind.Relative),
                Version = "1.0",
                MessageTypeIdentifier = "addressInfo",
                TimeStamp = _testdate,
                Message = "Test"
            }
        };
        var sqsMessage = new Message
        {
            Body = JsonSerializer.Serialize(messageEnvelope),
            ReceiptHandle = "receipt-handle"
        };
        messageSerializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(@"{}");
        messageSerializer.Setup(x => x.Deserialize(It.IsAny<string>(), It.IsAny<Type>())).Throws(new JsonException("Test exception"));

        var exception = await Assert.ThrowsAsync<FailedToCreateMessageEnvelopeException>(async () => await envelopeSerializer.ConvertToEnvelopeAsync(sqsMessage));

        Assert.Equal("Failed to create MessageEnvelope", exception.Message);
        if (dataMessageLogging)
        {
            Assert.NotNull(exception.InnerException);
        }
        else
        {
            Assert.Null(exception.InnerException);
        }
    }

    [Fact]
    public async Task ConvertToEnvelope_NullSubscriberMapping_ThrowsException()
    {
        // ARRANGE
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        var envelopeSerializer = serviceProvider.GetRequiredService<IEnvelopeSerializer>();
        var messageEnvelope = new MessageEnvelope<AddressInfo>
        {
            Id = "66659d05-e4ff-462f-81c4-09e560e66a5c",
            Source = new Uri("/aws/messaging", UriKind.Relative),
            Version = "1.0",
            MessageTypeIdentifier = "unknownMessageType", // Using an unknown message type
            TimeStamp = _testdate,
            Message = new AddressInfo
            {
                Street = "Prince St",
                Unit = 123,
                ZipCode = "00001"
            }
        };

        var sqsMessage = new Message
        {
            Body = await envelopeSerializer.SerializeAsync(messageEnvelope),
            ReceiptHandle = "receipt-handle"
        };

        // ACT & ASSERT
        var exception = await Assert.ThrowsAsync<FailedToCreateMessageEnvelopeException>(
            async () => await envelopeSerializer.ConvertToEnvelopeAsync(sqsMessage)
        );

        // Verify the exception message
        Assert.Equal("Failed to create MessageEnvelope", exception.Message);

        // Verify the inner exception type and message
        Assert.IsType<InvalidDataException>(exception.InnerException);
        var innerException = exception.InnerException as InvalidDataException;
        Assert.Contains("'unknownMessageType' is not a valid subscriber mapping.", innerException.Message);
        Assert.Contains("Available mappings:", innerException.Message);
        Assert.Contains("addressInfo", innerException.Message);
    }

}

public class MockSerializationCallback : ISerializationCallback
{
    public ValueTask PreSerializationAsync(MessageEnvelope messageEnvelope)
    {
        messageEnvelope.Metadata["Is-Delivered"] = JsonSerializer.SerializeToElement(false);
        return ValueTask.CompletedTask;
    }

    public ValueTask<string> PostSerializationAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var encodedString = Convert.ToBase64String(bytes);
        return new ValueTask<string>(encodedString);
    }

    public ValueTask<string> PreDeserializationAsync(string message)
    {
        var bytes = Convert.FromBase64String(message);
        var decodedString = Encoding.UTF8.GetString(bytes);
        return new ValueTask<string>(decodedString);
    }

    public ValueTask PostDeserializationAsync(MessageEnvelope messageEnvelope)
    {
        messageEnvelope.Metadata["Is-Delivered"] = JsonSerializer.SerializeToElement(true);
        return ValueTask.CompletedTask;
    }
}
