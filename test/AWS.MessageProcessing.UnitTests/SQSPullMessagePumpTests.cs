namespace AWS.MessageProcessing.UnitTests
{
    public class SQSPullMessagePumpTests
    {
        [Fact]
        public async Task TestMessageHasBeenProcessedThroughMessagePump()
        {
            var services = CreateDefaultServiceCollection();

            var sqsClientMock = CreateSQSMockClient(
                new ReceiveMessageResponse
                {
                    Messages = new List<Message>
                    { 
                        new Message
                        {
                            Body = File.ReadAllText("./DataFiles/FooBarMessage.json")
                        }
                    }
                });

            services.AddSingleton<IAmazonSQS>(sqsClientMock);

            services.AddAWSMessageBus(builder =>
            {
                builder.AddSQSPoller("http://dummyqueue.amazon.com/");
                builder.AddSubscriberHandler<ProcessIdCheckerHandler, FooBarMessage>();
            });
            var provider = services.BuildServiceProvider();

            var source = new CancellationTokenSource();
            var pump = ActivatorUtilities.CreateInstance<MessagePumpService>(provider);

            var task = pump.RunSQSPollersAsync(source.Token);

            await Task.Delay(1000);
            source.Cancel();

            Assert.True(ProcessIdCheckerHandler.ProcessIds.TryGetValue("1234", out var count));
            Assert.Equal(1, count);
        }

        public class ProcessIdCheckerHandler : IMessageHandler<FooBarMessage>
        {
            public static ConcurrentDictionary<string, int> ProcessIds = new ConcurrentDictionary<string, int>();
            public Task<bool> HandleAsync(MessageEnvelope<FooBarMessage> messageEnvelope, CancellationToken token = default)
            {
                ProcessIds.AddOrUpdate(messageEnvelope.Id, id => 1, (id, count) => count + 1);

                return Task.FromResult(true);
            }
        }
    }
}
