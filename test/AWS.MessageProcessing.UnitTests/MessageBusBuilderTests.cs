using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using AWS.MessageProcessing.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;



namespace AWS.MessageProcessing.UnitTests
{
    public class MessageBusBuilderTests
    {
        [Fact]
        public void AddSubscriberHandlerTestWithoutMessageTypeIdentifier()
        {
            IServiceCollection services = CreateDefaultServiceCollection();
            services.AddAWSMessageBus(builder =>
            {
                builder.AddSubscriberHandler<TestHandler, TestMessage>();
            });

            var provider = services.BuildServiceProvider();
            var configuration = provider.GetService<IMessagingConfiguration>();
            Assert.NotNull(configuration);

            Assert.Single(configuration.HandleMappings);
            Assert.Equal(typeof(TestHandler), configuration.HandleMappings[0].HandlerType);
            Assert.Equal(typeof(TestMessage), configuration.HandleMappings[0].MessageType);
            Assert.Equal("AWS.MessageProcessing.UnitTests.MessageBusBuilderTests+TestMessage", configuration.HandleMappings[0].MessageTypeIdentifier);

            Assert.NotNull(provider.GetService<TestHandler>());
        }

        [Fact]
        public void AddSubscriberHandlerTestWithMessageTypeIdentifier()
        {
            IServiceCollection services = CreateDefaultServiceCollection();
            services.AddAWSMessageBus(builder =>
            {
                builder.AddSubscriberHandler<TestHandler, TestMessage>("TestMessage");
            });

            var provider = services.BuildServiceProvider();
            var configuration = provider.GetService<IMessagingConfiguration>();
            Assert.NotNull(configuration);

            Assert.Single(configuration.HandleMappings);
            Assert.Equal(typeof(TestHandler), configuration.HandleMappings[0].HandlerType);
            Assert.Equal(typeof(TestMessage), configuration.HandleMappings[0].MessageType);
            Assert.Equal("TestMessage", configuration.HandleMappings[0].MessageTypeIdentifier);

            Assert.NotNull(provider.GetService<TestHandler>());
        }

        [Fact]
        public void AddSQSPollerTest()
        {
            var queueUrl = "https://sqs.us-west-2.amazonaws.com/123412341234/my-queue";
            IServiceCollection services = CreateDefaultServiceCollection();
            services.AddAWSMessageBus(builder =>
            {
                builder.AddSQSPoller(queueUrl);
            });

            var provider = services.BuildServiceProvider();
            var configuration = provider.GetService<IMessagingConfiguration>();

            Assert.Single(configuration.SQSPollerConfigurations);
            Assert.Equal(queueUrl, configuration.SQSPollerConfigurations[0].QueueUrl);

            // Make sure since SQS pollers were added that IAmazonSQS was added to services
            Assert.NotNull(provider.GetService<IAmazonSQS>());
        }


        [Fact]
        public void AddSQSPollerTestWithCustomIAmazonSQS()
        {
            var sqsClient = new Mock<IAmazonSQS>().Object;
            var queueUrl = "https://sqs.us-west-2.amazonaws.com/123412341234/my-queue";
            IServiceCollection services = CreateDefaultServiceCollection();
            services.AddSingleton<IAmazonSQS>(sqsClient);
            services.AddAWSMessageBus(builder =>
            {
                builder.AddSQSPoller(queueUrl);
            });

            var provider = services.BuildServiceProvider();
            var configuration = provider.GetService<IMessagingConfiguration>();

            Assert.Single(configuration.SQSPollerConfigurations);
            Assert.Equal(queueUrl, configuration.SQSPollerConfigurations[0].QueueUrl);

            // Make sure sqs client in DI is still the one we added in the test.
            var registeredSqsClient = provider.GetService<IAmazonSQS>();
            Assert.True(object.ReferenceEquals(sqsClient, registeredSqsClient));
        }


        public class TestMessage
        {
            public string FooData { get; set; }
        }

        public class TestHandler : IMessageHandler<TestMessage>
        {
            public Task<bool> HandleAsync(MessageEnvelope<TestMessage> messageEnvelope, CancellationToken token = default)
            {
                return Task.FromResult(true);
            }
        }
    }
}
