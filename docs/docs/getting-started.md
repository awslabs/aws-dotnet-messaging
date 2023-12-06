# Getting Started

Add the `AWS.Messaging` NuGet package to your project
```
dotnet add package AWS.Messaging
```

The framework integrates with Microsoft's dependency injection (DI) container. This can be done in your application startup by calling `AddAWSMessageBus` to add the AWS Message Processing Framework for .NET to the DI container.

```csharp
var builder = WebApplication.CreateBuilder(args);
// Register the AWS Message Processing Framework for .NET
builder.Services.AddAWSMessageBus(builder =>
{
    // Register a .NET object to an SQS Queue
    builder.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd");
});
```

The message bus allows for adding the necessary configuration to support applications that are publishing messages, processing messages or doing both at the same time.

Sample configuration for a `Publisher`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register the AWS Message Processing Framework for .NET
builder.Services.AddAWSMessageBus(builder =>
{
    // Register a .NET object to an SQS Queue
    builder.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd");

    // Register a .NET object to an SNS Topic
    builder.AddSNSPublisher<OrderInfo>("arn:aws:sns:us-west-2:012345678910:MyAppProd");

    // Register a .NET object to an EventBridge Event Bus
    builder.AddEventBridgePublisher<FoodItem>("arn:aws:events:us-west-2:012345678910:event-bus/default");

    // Configure serialization options
    builder.ConfigureSerializationOptions(options =>
    {
        options.SystemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    });
});
```

Once you have registered the framework in the DI container, all you need to publish a message is inject the `IMessagePublisher` into your code and call the `PublishAsync` method.

Here is an example showing a sample publisher for a hypothetical `ChatMessage` message.

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
        // Add business and validation logic here

        if (message == null)
        {
            return BadRequest("A chat message was not used.");
        }
        if (string.IsNullOrEmpty(message.MessageDescription))
        {
            return BadRequest("The MessageDescription cannot be null or empty.");
        }

        await _messagePublisher.PublishAsync(message);

        return Ok();
    }
}
```

If your application is a subscriber, your need to implement a message handler `IMessageHandler` interface for the message you wish to process. The message type and message handler are set up in the project startup.

Sample configuration for a `Subscriber`:

```csharp
await Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Register the AWS Message Processing Framework for .NET
        services.AddAWSMessageBus(builder =>
        {
            // Register an SQS Queue for the message pump to poll for messages
            builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MyAppProd");

            // Register all IMessageHandler implementations with the message type they should process
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });
    })
    .Build()
    .RunAsync();
```

Here is an example showing a sample message handler for a hypothetical `ChatMessage` message.

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

        var message = messageEnvelope.Message;

        Console.WriteLine($"Message Description: {message.MessageDescription}");

        return Task.FromResult(MessageProcessStatus.Success());
    }
}
```

## Using service-specific publishers

The AWS Message Processing Framework for .NET exposes service-specific publishers for the supported services SQS, SNS and EventBridge. These publishers are accessible through the DI container via the types `ISQSPublisher`, `ISNSPublisher` and `IEventBridgePublisher`. When using these publishers, you have access to service-specific options/configurations that can be set when publishing a message.

An example of using `ISQSPublisher` to set SQS-specific options:
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
        if (message == null)
        {
            return BadRequest("A chat message was not used.");
        }
        if (string.IsNullOrEmpty(message.MessageDescription))
        {
            return BadRequest("The MessageDescription cannot be null or empty.");
        }

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

The same can be done for `SNS` and `EventBridge` publishers using `ISNSPublisher` and `IEventBridgePublisher` respectively:
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