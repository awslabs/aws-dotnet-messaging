namespace AWS.MessageProcessing.UnitTests
{
    public class HandlerInvokerTests
    {
        [Fact]
        public async Task InvokeHandler()
        {
            IServiceCollection services = CreateDefaultServiceCollection();
            services.AddAWSMessageBus(builder =>
            {
                builder.AddSubscriberHandler<FooBarHandler, FooBarMessage>();
            });            

            var provider = services.BuildServiceProvider();
            var invoker = ActivatorUtilities.CreateInstance<HandlerInvoker>(provider);

            var messageEnvelope = new MessageEnvelope<FooBarMessage>();
            Assert.True(await invoker.InvokeAsync(messageEnvelope, typeof(FooBarMessage), typeof(FooBarHandler)));
        }

        [Fact]
        public async Task InvokeHandlerThrowsApplicationException()
        {
            IServiceCollection services = CreateDefaultServiceCollection();
            services.AddAWSMessageBus(builder =>
            {
                builder.AddSubscriberHandler<FooBarHandler, FooBarMessage>();
            });

            var provider = services.BuildServiceProvider();
            var invoker = ActivatorUtilities.CreateInstance<HandlerInvoker>(provider);

            var messageEnvelope = new MessageEnvelope<FooBarMessage>() {Id = "error" };
            await Assert.ThrowsAsync<ApplicationException>(async () => await invoker.InvokeAsync(messageEnvelope, typeof(FooBarMessage), typeof(FooBarHandler)));
        }
    }
}
