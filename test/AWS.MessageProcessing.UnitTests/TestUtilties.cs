using Microsoft.Extensions.Logging;

namespace AWS.MessageProcessing.UnitTests
{
    public static class TestUtilties
    {
        public static IServiceCollection CreateDefaultServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Trace);
            });
            return services;
        }

        public static IAmazonSQS CreateSQSMockClient(params ReceiveMessageResponse[] responses)
        {
            var sqsClientMock = new Mock<IAmazonSQS>();

            int callCount = 0;

            sqsClientMock.Setup(client => client.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .Returns((ReceiveMessageRequest r, CancellationToken token) =>
                {
                    if (responses.Length == callCount)
                        throw new Exception("Ran out of mock responses");

                    var response = responses[callCount];

                    if(response.ResponseMetadata == null)
                    {
                        response.ResponseMetadata = new Amazon.Runtime.ResponseMetadata { RequestId = Guid.NewGuid().ToString() };
                    }

                    callCount++;
                    return Task.FromResult(response);
                });

            return sqsClientMock.Object;
        }
    }
}
