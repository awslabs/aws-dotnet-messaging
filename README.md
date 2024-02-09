# AWS Message Processing Framework for .NET
[![nuget](https://img.shields.io/nuget/v/AWS.Messaging.svg) ![downloads](https://img.shields.io/nuget/dt/AWS.Messaging.svg)](https://www.nuget.org/packages/AWS.Messaging/)
[![build status](https://img.shields.io/github/actions/workflow/status/awslabs/aws-dotnet-messaging/aws-ci.yml?branch=dev)](https://github.com/awslabs/aws-dotnet-messaging/actions/workflows/aws-ci.yml)

**Notice:** *This library is still in active development and is meant for early access and feedback purposes only. It should not be used in production environments, and any releases before 1.0.0 might include breaking changes.*

The **AWS Message Processing Framework for .NET** is an AWS-native framework that simplifies the development of .NET message processing applications that use AWS services, such as Amazon Simple Queue Service (SQS), Amazon Simple Notification Service (SNS), and Amazon EventBridge. The framework reduces the amount of boiler-plate code developers need to write, allowing you to focus on your business logic when publishing and consuming messages.
* For publishers, the framework serializes the message from a .NET object to a [CloudEvents](https://cloudevents.io/)-compatible message, and then wraps that in the service-specific AWS message. It then publishes the message to the configured SQS queue, SNS topic, or EventBridge event bus. 
* For consumers, the framework deserializes the message to its .NET object and routes it to the appropriate business logic. The framework also keeps track of the message visibility while it is being processed (to avoid processing a message more than once), and deletes the message from the queue when completed. The framework supports consuming messages in both long-running polling processes and in AWS Lambda functions.

## Project Status

The framework is currently under active development. It already supports:
* Publishing to SQS, SNS, and EventBridge
* Handling SQS messages in a long-running, polling process
* Handling SQS messages in AWS Lambda functions
* Handling messages from [FIFO (first-in-first-out) queues](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-fifo-queues.html), and respecting group ordering
* OpenTelemetry Instrumentation
* Customizing serialization

Features to be added:
* AWS X-Ray Instrumentation
* Performance and error hardening

# Getting started

Add the `AWS.Messaging` NuGet package to your project:
```
dotnet add package AWS.Messaging --prerelease
```

The framework integrates with .NET's [dependency injection (DI) service container](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection). You can configure the framework during your application's startup by calling `AddAWSMessageBus` to add it to the DI container.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the AWS Message Processing Framework for .NET
builder.Services.AddAWSMessageBus(builder =>
{
    // Register that you'll publish messages of type ChatMessage to an existing queue
    builder.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd");
});
```

The framework supports publishing one or more message types, processing one or more message types, or doing both in the same application.

# Publishing Messages

The following code shows a configuration for an application that is publishing different message types to different AWS services.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the AWS Message Processing Framework for .NET
builder.Services.AddAWSMessageBus(builder =>
{
    // Register that you'll publish messages of type ChatMessage to an existing queue
    builder.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd");

    // Register that you'll publish messages of type OrderInfo to an existing SNS topic
    builder.AddSNSPublisher<OrderInfo>("arn:aws:sns:us-west-2:012345678910:MyAppProd");

    // Register that you'll publish messages of type FoodItem to an existing EventBridge bus
    builder.AddEventBridgePublisher<FoodItem>("arn:aws:events:us-west-2:012345678910:event-bus/default");
});
```

Once you have registered the framework during startup, inject the `IMessagePublisher` into your code and call its `PublishAsync` method to publish a message. In the following example, an ASP.NET MVC controller receives a `ChatMessage` from the user and then publishes that same object to SQS.

```csharp
[ApiController]
[Route("[controller]")]
public class PublisherController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    public PublisherController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpPost("chatmessage", Name = "Chat Message")]
    public async Task<IActionResult> PublishChatMessage([FromBody] ChatMessage message)
    {
        // Perform business and validation logic on the ChatMessage here
        if (message == null)
        {
            return BadRequest("A chat message was not submitted. Unable to forward to the message queue.");
        }
        if (string.IsNullOrEmpty(message.MessageDescription))
        {
            return BadRequest("The MessageDescription cannot be null or empty.");
        }

        // Publish the message to SQS, using the injected IMessagePublisher
        await _messagePublisher.PublishAsync(message);

        return Ok();
    }
}
```
## Service-specific publishers

The  example shown above uses the generic `IMessagePublisher`, which can publish to any supported AWS service based on the configured message type. The framework also provides *service-specific publishers* for SQS, SNS and EventBridge. These specific publishers expose options that only apply to that service, and can be injected using the types `ISQSPublisher`, `ISNSPublisher` and `IEventBridgePublisher`. 

For example, when publishing messages to an SQS FIFO queue, you must set the appropriate [message group ID](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/FIFO-key-terms.html). The following code shows the `ChatMessage` example again, but now using an `ISQSPublisher` to set SQS-specific options.
```csharp
public class PublisherController : ControllerBase
{
    private readonly ISQSPublisher _sqsPublisher;

    public PublisherController(ISQSPublisher sqsPublisher)
    {
        _sqsPublisher = sqsPublisher;
    }

    [HttpPost("chatmessage", Name = "Chat Message")]
    public async Task<IActionResult> PublishChatMessage([FromBody] ChatMessage message)
    {
        // Perform business and validation logic on the ChatMessage here
        if (message == null)
        {
            return BadRequest("A chat message was not submitted. Unable to forward to the message queue.");
        }
        if (string.IsNullOrEmpty(message.MessageDescription))
        {
            return BadRequest("The MessageDescription cannot be null or empty.");
        }

        // Publish the message to SQS using the injected ISQSPublisher, with SQS-specific options
        await _sqsPublisher.PublishAsync(message, new SQSOptions
        {
            DelaySeconds = <delay-in-seconds>,
            MessageAttributes = <message-attributes>,
            MessageDeduplicationId = <message-deduplication-id>,
            MessageGroupId = <message-group-id>
        });

        return Ok();
    }
}
```

The same can be done for SNS and EventBridge, using `ISNSPublisher` and `IEventBridgePublisher` respectively.
```csharp
await _snsPublisher.PublishAsync(message, new SNSOptions
{
    Subject = <subject>,
    MessageAttributes = <message-attributes>,
    MessageDeduplicationId = <message-deduplication-id>,
    MessageGroupId = <message-group-id>
});
```
```csharp
await _eventBridgePublisher.PublishAsync(message, new EventBridgeOptions
{
    DetailType = <detail-type>,
    Resources = <resources>,
    Source = <source>,
    Time = <time>,
    TraceHeader = <trace-header>
});
```

# Consuming Messages

To consume messages, implement a message handler using the `IMessageHandler` interface for each message type you wish to process. The mapping between message types and message handlers is configured in the project startup.

```csharp
await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register the AWS Message Processing Framework for .NET
        services.AddAWSMessageBus(builder =>
        {
            // Register an SQS Queue that the framework will poll for messages
            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd");

            // Register all IMessageHandler implementations with the message type they should process. 
            // Here messages that match our ChatMessage .NET type will be handled by our ChatMessageHandler
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });
    })
    .Build()
    .RunAsync();
```

The following code shows a sample message handler for a `ChatMessage` message. 

```csharp
public class ChatMessageHandler : IMessageHandler<ChatMessage>
{
    public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ChatMessage> messageEnvelope, CancellationToken token = default)
    {
        // Add business and validation logic here
        if (messageEnvelope == null)
        {
            return Task.FromResult(MessageProcessStatus.Failed());
        }

        if (messageEnvelope.Message == null)
        {
            return Task.FromResult(MessageProcessStatus.Failed());
        }

        ChatMessage message = messageEnvelope.Message;

        Console.WriteLine($"Message Description: {message.MessageDescription}");

        // Return success so the framework will delete the message from the queue
        return Task.FromResult(MessageProcessStatus.Success());
    }
}
```

The outer `MessageEnvelope` contains metadata used by the framework. Its `message` property is the message type (in this case `ChatMessage`). 

You can return `MessageProcessStatus.Success()` to indicate that the message was processed successfully and the framework will delete the message from the SQS queue. When returning `MessageProcessStatus.Failed()` the message will remain in the queue, where it can be processed again or moved to a [dead-letter queue](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-dead-letter-queues.html) if configured.

## Handling Messages in a Long-Running Process
You can call `AddSQSPoller` with an SQS queue URL to start a long-running [`BackgroundService`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice) that will continuously poll the queue and process messages.

```csharp
await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register the AWS Message Processing Framework for .NET
        services.AddAWSMessageBus(builder =>
        {
            // Register an SQS Queue that the framework will poll for messages
            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd", options => 
            {
                // The maximum number of messages from this queue that the framework will process concurrently on this client
                options.MaxNumberOfConcurrentMessages = 10;

                // The duration each call to SQS will wait for new messages
                options.WaitTimeSeconds = 20; 
            });

            // Register all IMessageHandler implementations with the message type they should process
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });
    })
    .Build()
    .RunAsync();
```

### Configuring the SQS Message Poller
The SQS message poller can be configured by the `SQSMessagePollerOptions` when calling `AddSQSPoller`.
* `MaxNumberOfConcurrentMessages` - The maximum number of messages from the queue to process concurrently. The default value is `10`.
* `WaitTimeSeconds` - The duration (in seconds) for which the `ReceiveMessage` SQS call waits for a message to arrive in the queue before returning. If a message is available, the call returns sooner than `WaitTimeSeconds`. The default value is `20`.

#### Message Visibility Timeout Handling
SQS messages have a [visibility timeout](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html) period. When one consumer begins handling a given message, it remains in the queue but is hidden from other consumers to avoid processing it more than once. If the message is not handled and deleted before becoming visible again, another consumer may attempt to handle the same message.

The framework will track and attempt to extend the visibility timeout for messages that it is currently handling. You can configure this behavior on the `SQSMessagePollerOptions` when calling `AddSQSPoller`.
* `VisibilityTimeout` - The duration in seconds that received messages are hidden from subsequent retrieve requests. The default value is `30`.
* `VisibilityTimeoutExtensionThreshold` - When a message's visibility timeout is within this many seconds of expiring, the framework will extend the visibility timeout (by another `VisibilityTimeout` seconds). The default value is `5`.
* `VisibilityTimeoutExtensionHeartbeatInterval`- How often in seconds that the framework will check for messages that are within `VisibilityTimeoutExtensionThreshold` seconds of expiring, and then extend their visibility timeout. The default value is `1`.

In the following example the framework will check every 1 second for messages that are still being handled. For those messages within 5 seconds of becoming visible again, the framework will automatically extend the visibility timeout of each message by another 30 seconds.
```csharp
 builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd", options => 
{
    options.VisibilityTimeout = 30;
    options.VisibilityTimeoutExtensionThreshold = 5;
    VisibilityTimeoutExtensionHeartbeatInterval = 1;
});
```

## Handling Messages in AWS Lambda Functions
You can use the AWS Message Processing Framework for .NET with [SQS's integration with Lambda](https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html). This is provided by the `AWS.Messaging.Lambda` package. Refer to its [README](https://github.com/awslabs/aws-dotnet-messaging/blob/main/src/AWS.Messaging.Lambda/README.md) to get started.

# Telemetry
The AWS Message Processing Framework for .NET is instrumented for OpenTelemetry to log [traces](https://opentelemetry.io/docs/concepts/signals/traces/) for each message that is published or handled by the framework. This is provided by the `AWS.Messaging.Telemetry.OpenTelemetry` package. Refer to its [README](https://github.com/awslabs/aws-dotnet-messaging/blob/main/src/AWS.Messaging.Telemetry.OpenTelemetry/README.md) to get started.

# Customization
The framework builds, sends, and handles messages in three different "layers":
1. At the outermost layer, the framework builds the AWS-native request or response specific to a service. With SQS for example, it builds [`SendMessage`](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessage.html) requests, and works with the [`Message`](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_Message.html) objects that are defined by the service.
2. Inside the SQS request and response, it sets the `MessageBody` element (or `Message` for SNS or `Detail` for EventBridge) to a [JSON-formatted CloudEvent](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md). This contains metadata set by the framework that is accessible on the `MessageEnvelope` object when handling a message.
3. At the innermost layer, the `data` attribute inside the CloudEvent JSON object contains a JSON serialization of the .NET object that was sent or received as the message.

```json
{
    "id":"b02f156b-0f02-48cf-ae54-4fbbe05cffba",
    "source":"/aws/messaging",
    "specversion":"1.0",
    "type":"Publisher.Models.ChatMessage",
    "time":"2023-11-21T16:36:02.8957126+00:00",
    "data":"<the ChatMessage object serialized as JSON>"
}
```

You can customize how the message envelope is configured and read:
* `"id"` uniquely identifies the message. By default it is set to a new GUID, but this can be overridden by implementing your own `IMessageIdGenerator` and injecting that into the DI container.
* `"type"` controls how the message is routed to handlers. By default this uses the full name of the .NET type that corresponds to the message. You can override this via the `messageTypeIdentifier` parameter when mapping the message type to the destination via `AddSQSPublisher`, `AddSNSPublisher`, or `AddEventBridgePublisher`.
* `"source"` indicates which system or server sent the message. 
     * This will be the function name if publishing from AWS Lambda, the cluster name and task ARN if on Amazon ECS, the instance ID if on Amazon EC2, otherwise a fallback value of `/aws/messaging`.
     * You can override this via `AddMessageSource` or `AddMessageSourceSuffix` on the `MessageBusBuilder`.
* `"time"` set to the current DateTime in UTC. This can be overridden by implementing your own `IDateTimeHandler` and injecting that into the DI container.
* `"data"` contains a JSON representation of the .NET object that was sent or received as the message:
    * `ConfigureSerializationOptions` on `MessageBusBuilder` allows you to configure the [`System.Text.Json.JsonSerializerOptions`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions) that will be used when serializing and deserializing the message.
    * To inject additional attributes or transform the message envelope once the framework builds it, you can implement `ISerializationCallback` and register that via `AddSerializationCallback` on `MessageBusBuilder`.

# Getting Help
For feature requests or issues using this framework please open an [issue in this repository](https://github.com/aws/aws-dotnet-messaging/issues).

# Contributing
We welcome community contributions and pull requests. See [CONTRIBUTING.md](./CONTRIBUTING.md) for information on how to submit code.

# Security
The AWS Message Processing Framework for .NET relies on the [AWS SDK for .NET](https://github.com/aws/aws-sdk-net) for communicating with AWS. Refer to the [security section](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/security.html) in the [AWS SDK for .NET Developer Guide](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/welcome.html) for more information.

The framework does not log data messages sent by the user for security purposes. If users want to enabled this functionality for debugging purposes, you need to call `EnableDataMessageLogging()` in the AWS Message Bus as follows:
```csharp
builder.Services.AddAWSMessageBus(bus =>
{
    builder.EnableDataMessageLogging();
});
```

If you discover a potential security issue, refer to the [security policy](https://github.com/awslabs/aws-dotnet-messaging/security/policy) for reporting information.

# Additional Resources
* [AWS Message Processing Framework for .NET Design Document](./docs/docs/design/message-processing-framework-design.md)
* [Sample Applications](https://github.com/awslabs/aws-dotnet-messaging/tree/main/sampleapps) in this repo contains samples of a publisher service, long-running subscriber service, and Lambda function handlers.

# License

This project is licensed under the Apache-2.0 License.
