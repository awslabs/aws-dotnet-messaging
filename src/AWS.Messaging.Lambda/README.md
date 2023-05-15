# AWS Lambda plugin for AWS Message Processing Framework for .NET

**Notice:** *This library is still in early active development and is not ready for use beyond experimentation.*

This package is a plugin for the [AWS Message Processing Framework for .NET](https://github.com/awslabs/aws-dotnet-messaging) that allows a .NET Lambda function
to be the the subscriber of messages for the AWS Message Processing Framework for .NET.

In AWS Lambda the Lambda service takes care of reading the messages from the SQS queue. This plugin allows the messages in the incoming Lambda event to be feed
into the AWS Message Processing Framework so it can dispatch the messages to the `IMessageHandler`.

## Example

The example below uses the [.NET Amazon Lambda Annotations](https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.Annotations) framework
which makes it easy to setup .NET's dependency injection.

In the `Startup` class add a call to `AddAWSMessageBus` to configure the AWS Message Processing Framework with the `IMessageHandler` for each message type you
expect the Lambda function to process. To inject the required services for using Lambda with the framework add a call to `AddLambdaMessageProcessor`.
Publishers can also be configured in if you expect the Lambda function to publish messages.
```
[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAWSMessageBus(builder =>
        {
            builder.AddMessageHandler<OrderHandler, OrderInfo>();

            builder.AddLambdaMessageProcessor(options =>
            {
                options.MaxNumberOfConcurrentMessages = 4;
            });
        });
    }
}
```

In the Lambda function itself you need to inject the `ILambdaMessaging` service. This service provides the entry point for the Lambda function
to pass in the `SQSEvent` sent in by the Lambda service. If you Lambda function is configured for partial failure response use the
`ProcessLambdaEventWithBatchResponseAsync` and return the instance of `SQSBatchResponse`. If partial failure response is not enabled
use the `ProcessLambdaEventAsync` method.

```
public class Function
{

    [LambdaFunction(Policies = "AWSLambdaSQSQueueExecutionRole")]
    public async Task<SQSBatchResponse> FunctionHandler([FromServices] ILambdaMessaging messaging, SQSEvent evnt, ILambdaContext context)
    {
        return await messaging.ProcessLambdaEventWithBatchResponseAsync(evnt, context);
    }
}
```
# Useful Links
* [AWS Message Processing Framework for .NET Design Document](./docs/design/message-processing-framework-design.md)

# Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

# License

This project is licensed under the Apache-2.0 License.
