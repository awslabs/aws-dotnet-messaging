namespace AWS.MessageProcessing.UnitTests
{
    public class SerializationTests
    {
        [Fact]
        public void MessageSerialization()
        {
            IMessageSerialization serializer = new DefaultMessageSerialization();

            var user1 = new User
            {
                FirstName = "Han",
                LastName = "Solo"
            };

            var json = serializer.Serialize(user1);

            var user2 = serializer.Deserialize<User>(json);

            Assert.Equal(user1.FirstName, user2.FirstName);
            Assert.Equal(user1.LastName, user2.LastName);
        }

        [Fact]
        public void EnvelopeSerialization()
        {
            var envelopeMessageJson = File.ReadAllText("./DataFiles/UserMessage.json");

            IEnvelopeSerialization envelopeSerializer = new DefaultEnvelopeSerialization();

            var flatEnvelopeMessage = envelopeSerializer.Deserialize(new Message { Body = envelopeMessageJson });

            Assert.Equal("1234", flatEnvelopeMessage.Id);
            Assert.Equal("TheSourceOfMessage", flatEnvelopeMessage.Source);
            Assert.Equal(new DateTime(2022, 2, 4), flatEnvelopeMessage.CreatedTimeStamp);
            Assert.Equal("AWS.MessageProcessing.UnitTests.SerializationTests.User", flatEnvelopeMessage.MessageType);

            IMessageSerialization messageSerializer = new DefaultMessageSerialization();

            var user = messageSerializer.Deserialize<User>(flatEnvelopeMessage.RawMessage);
            Assert.Equal("Han", user.FirstName);
            Assert.Equal("Solo", user.LastName);
        }

        [Fact]
        public void TestSerializationUtilities()
        {
            var services = CreateDefaultServiceCollection();
            services.AddAWSMessageBus(builder =>
            {
                builder.AddSubscriberHandler<FooBarHandler, FooBarMessage>();
            });

            var provider = services.BuildServiceProvider();
            var utilites = ActivatorUtilities.CreateInstance<SerializationUtilties>(provider);

            var envelopeMessageJson = File.ReadAllText("./DataFiles/FooBarMessage.json");
            var sqsMessage = new Message { Body = envelopeMessageJson };
            var convertResults = utilites.ConvertToEnvelopeMessage(sqsMessage);

            var messageEnvelope = convertResults.MessageEnvelope as MessageEnvelope<FooBarMessage>;
            Assert.NotNull(messageEnvelope);
            Assert.Equal("Luke", messageEnvelope.Message.Foo);
            Assert.Equal("Leia", messageEnvelope.Message.Bar);
            Assert.Equal("1234", messageEnvelope.Id);
        }


        public class User
        {
            public string? FirstName { get; set; }

            public string? LastName { get; set; }
        }
    }
}