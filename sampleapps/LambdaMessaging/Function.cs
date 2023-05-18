using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using AWS.Messaging.Lambda;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaMessaging;

/// <summary>
/// This sample uses the Amazon.Lambda.Annotations framework to use attributes to configure
/// Lambda function and setup the dependency injection framework. For more information
/// on Amazon.Lambda.Annotations check out the repository:
/// https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.Annotations
/// </summary>
public class Function
{
    private readonly ILambdaMessaging _messaging;

    /// <summary>
    /// Creates an instance and injects the ILambdaMessaging service
    /// from the dependency injection framework configured in the Startup class.
    /// </summary>
    /// <param name="messaging"></param>
    public Function(ILambdaMessaging messaging)
    {
        _messaging = messaging;
    }

    /// <summary>
    /// Lambda function that sends the incoming Lambda SQS Event into the .NET Message Framework.
    /// This function returns an <see cref="Amazon.Lambda.SQSEvents.SQSBatchResponse"/> to support partial batch failure.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    [LambdaFunction(Policies = "AWSLambdaSQSQueueExecutionRole")]
    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        return await _messaging.ProcessLambdaEventWithBatchResponseAsync(evnt, context);
    }
}
