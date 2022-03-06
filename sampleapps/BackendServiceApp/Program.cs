using BackendServiceApp;
using CommonModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("appsettings.json");
    })    
    .ConfigureServices(services =>
    {

        services.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/626492997873/TestMessageProcessingFramework");

            builder.AddSubscriberHandler<OrderProcessorHandler, OrderInfo>();
        });
    })
    .Build()
    .RunAsync();