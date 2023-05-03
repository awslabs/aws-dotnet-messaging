using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Messaging.Lambda;
using Microsoft.Extensions.DependencyInjection;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaMessaging;

public class Function
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        _serviceProvider = ConfigureServices();
    }


    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var serviceProvider = ConfigureServices();
        var lambdaMessagePump = serviceProvider.GetRequiredService<ILambdaMessagePump>();
        await lambdaMessagePump.ExecuteAsync(sqsEvent, new CancellationTokenSource().Token);
    }

    private IServiceProvider ConfigureServices()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }
}
