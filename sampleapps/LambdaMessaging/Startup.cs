using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LambdaMessaging;

[LambdaStartup]
public class Startup
{
    public HostApplicationBuilder ConfigureHostBuilder()
    {
        var builder = new HostApplicationBuilder();
        builder.Services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.AddLambdaLogger();
        });
        builder.Services.AddAWSMessageBus(b =>
        {
            b.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");

            b.AddLambdaMessageProcessor(options =>
            {
                options.MaxNumberOfConcurrentMessages = 2;
            });
        });

        return builder;
    }
}
