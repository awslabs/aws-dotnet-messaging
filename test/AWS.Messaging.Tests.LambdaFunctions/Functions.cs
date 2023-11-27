using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Messaging.Lambda;
using AWS.Messaging.Tests.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWS.Messaging.Tests.LambdaFunctions;

public class Functions
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Functions()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddMessageHandler<TransactionInfoHandler, TransactionInfo>("TransactionInfo");

            builder.AddLambdaMessageProcessor();
        });

        serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
         });

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }


    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task LambdaEventHandler(SQSEvent evnt, ILambdaContext context)
    {
        var messaging = _serviceProvider.GetRequiredService<ILambdaMessaging>();

        await messaging.ProcessLambdaEventAsync(evnt, context);
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<SQSBatchResponse> LambdaEventWithBatchResponseHandler(SQSEvent evnt, ILambdaContext context)
    {
        var messaging = _serviceProvider.GetRequiredService<ILambdaMessaging>();

        return await messaging.ProcessLambdaEventWithBatchResponseAsync(evnt, context);
    }
}

public class TransactionInfoHandler : IMessageHandler<TransactionInfo>
{
    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<TransactionInfo> messageEnvelope, CancellationToken token = default)
    {
        // Wait for the delay specified in the message
        await Task.Delay(messageEnvelope.Message.WaitTime, token);

        if (messageEnvelope.Message.ShouldFail)
        {
            return MessageProcessStatus.Failed();
        }

        // Log the received message's ID to the Lambda's logs, the test will assert via CloudWatchLogs
        if (!string.IsNullOrEmpty(messageEnvelope.SQSMetadata?.MessageGroupId))
        {
            LambdaLogger.Log($"Processed message with Id: {messageEnvelope.Message.TransactionId} as part of group {messageEnvelope.SQSMetadata?.MessageGroupId}");
        }
        else
        {
            LambdaLogger.Log($"Processed message with Id: {messageEnvelope.Message.TransactionId}");

        }

        return await Task.FromResult(MessageProcessStatus.Success());
    }
}
