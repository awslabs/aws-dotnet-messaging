using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LambdaSubscriberApp;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddLambdaLogger();
        });
        services.AddAWSMessageBus(builder =>
        {
            builder.AddMessageHandler<TransactionHandler, TransactionInfo>("transactionInfo");

            builder.AddLambdaMessageProcessor(options =>
            {
                options.MaxNumberOfConcurrentMessages = 2;
            });
        });
    }
}
