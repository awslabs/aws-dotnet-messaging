# AWS Lambda plugin for AWS Message Processing Framework for .NET
[![nuget](https://img.shields.io/nuget/v/AWS.Messaging.Lambda.svg) ![downloads](https://img.shields.io/nuget/dt/AWS.Messaging.Lambda.svg)](https://www.nuget.org/packages/AWS.Messaging.Lambda/)

**Notice:** *This library is in **developer preview**. It provides early access to upcoming features in the **AWS Message Processing Framework for .NET**. Any releases prior to 1.0.0 might include breaking changes.*

This package is a plugin for the [AWS Message Processing Framework for .NET](https://github.com/awslabs/aws-dotnet-messaging) that allows a .NET Lambda function to handle messages that were published by the framework.

In AWS Lambda, the service takes care of reading the messages from the SQS queue and invoking your Lambda functions with the message events. This plugin allows you to feed the incoming Lambda event to message processing framework so it can dispatch the messages to the `IMessageHandler`.

## Example

To get started, add the `AWS.Messaging.Lambda` NuGet package to your project:
```
dotnet add package AWS.Messaging.Lambda
```

The example shown below uses the [.NET Amazon Lambda Annotations](https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.Annotations) framework, which makes it easy to set up .NET's dependency injection.

In the `Startup` class, add a call to `AddAWSMessageBus` to configure the AWS Message Processing Framework with the `IMessageHandler` for each message type you expect the Lambda function to process. To inject the required services for using Lambda with the framework, add a call to `AddLambdaMessageProcessor`.
Publishers can also be configured if you expect the Lambda function to publish messages.
```
[LambdaStartup]
public class Startup
{
   public HostApplicationBuilder ConfigureHostBuilder()
    {
        var hostBuilder = new HostApplicationBuilder();
        
        hostBuilder.Services.AddAWSMessageBus(builder =>
        {
            builder.AddMessageHandler<OrderHandler, OrderInfo>();

            builder.AddLambdaMessageProcessor(options =>
            {
                options.MaxNumberOfConcurrentMessages = 4;
            });
        });
        
        return hostBuilder;
    }
}
```

In the Lambda function itself you need to inject the `ILambdaMessaging` service. This service provides the entry point for the Lambda function
to pass in the `SQSEvent` sent in by the Lambda service. If your Lambda function is configured for partial failure response use the
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

## Options
When calling `AddLambdaMessageProcessor` the following options are available to configure the framework.

* **MaxNumberOfConcurrentMessages**: The max number of messages the Lambda function will process at the same time.
The default value is `10`.
* **DeleteMessagesWhenCompleted**: When **not** using partial response failure with Lambda if this is set to `true`
then after each message has been successfully processed the framework will delete the message. The default value is `false`
which means the Lambda service will delete all of the messages in the Lambda event if the function invocation
was successful. If the function is configured for partial response failure this property is ignored.


# Useful Links
* [AWS Message Processing Framework for .NET Design Document](../../docs/docs/design/message-processing-framework-design.md)
* [Sample Applications](https://github.com/awslabs/aws-dotnet-messaging/tree/main/sampleapps) - contains sample applications of a publisher service, long-running subscriber service, Lambda function handlers, and using Polly to override the framework's built-in backoff logic.
* [Developer Guide](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/msg-proc-fw.html)
* [API Reference](https://awslabs.github.io/aws-dotnet-messaging/)
* [Introducing the AWS Message Processing Framework for .NET (Preview) Blog Post](https://aws.amazon.com/blogs/developer/introducing-the-aws-message-processing-framework-for-net-preview/) - walks through creating simple applications to send and receive SQS messages.

# Security

See [CONTRIBUTING](CONTRIBUTING.md#security-issue-notifications) for more information.

# License

This project is licensed under the Apache-2.0 License.
