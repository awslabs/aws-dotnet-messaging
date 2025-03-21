# AWS Message Processing Framework Subscriber Service Sample

This sample application demonstrates how to use the AWS Message Processing Framework for .NET to process messages from SQS queues with configurable polling and message handling.

## Overview

This sample demonstrates:
- Configuring and using SQS message pollers
- Implementing typed message handlers
- Configuration-based and code-based setup options
- OpenTelemetry integration for observability
- Configurable backoff policies for message processing
- Proper message handling patterns

## Prerequisites

- .NET 8.0 or later
- AWS Account with appropriate permissions
- Basic understanding of Amazon SQS and message processing

## Setup

### 1. Create AWS Resources

#### SQS Queue
1. Open the AWS Management Console
2. Navigate to Amazon SQS
3. Click "Create Queue"
4. Choose "Standard Queue"
5. Enter a queue name (e.g., "MPF")
6. Keep default settings for this demo
7. Click "Create Queue"
8. Copy the Queue URL - you'll need this later

### 2. Configure Message Processing

You can choose either configuration approach:

#### Option A: Code-based Configuration

In `Program.cs`, update the queue URL and keep these lines uncommented:
```csharp
builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MPF");
builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");
```
And keep this line commented:

```
// builder.LoadConfigurationFromSettings(context.Configuration);
```
#### Option B: Configuration-based (appsettings.json)

1. Comment out the code-based configuration in Program.cs
2. Uncomment the configuration loading:
```
builder.LoadConfigurationFromSettings(context.Configuration);
```
3. Update appsettings.json:
```
{
    "AWS.Messaging": {
        "MessageHandlers": [
            {
                "HandlerType": "SubscriberService.MessageHandlers.ChatMessageHandler",
                "MessageType": "SubscriberService.Models.ChatMessage",
                "MessageTypeIdentifier": "chatMessage"
            }
        ],
        "SQSPollers": [
            {
                "QueueUrl": "https://sqs.us-west-2.amazonaws.com/012345678910/MPF",
                "Options": {
                    "MaxNumberOfConcurrentMessages": 10,
                    "VisibilityTimeout": 20,
                    "WaitTimeSeconds": 20,
                    "VisibilityTimeoutExtensionHeartbeatInterval": 1,
                    "VisibilityTimeoutExtensionThreshold": 5
                }
            }
        ],
        "BackoffPolicy": "CappedExponential"
    }
}
```

### 3. Configure AWS Credentials

Ensure you have AWS credentials configured either through:

- AWS CLI
    
- Environment variables
    
- AWS credentials file
    
- IAM role (if running on AWS)
    

## Project Structure
```
SubscriberService/
├── MessageHandlers/
│   └── ChatMessageHandler.cs    # Message handler implementation
├── Models/
│   └── ChatMessage.cs          # Message type definition
├── Program.cs                  # Application entry point
└── appsettings.json           # Application configuration
```

## Running the Application

1. Build the project:
```
dotnet build
```
2. Run the application:
```
dotnet run
```

## Testing

You can test the service by sending messages to the SQS queue using the AWS Console, AWS CLI, or the companion PublisherAPI project.

### Using AWS CLI:
```
aws sqs send-message \
    --queue-url YOUR_QUEUE_URL \
    --message-body '{
        "type": "chatMessage",
        "id": "123",
        "source": "test",
        "specversion": "1.0",
        "time": "2024-01-01T00:00:00Z",
        "data": {
            "messageDescription": "Test message"
        }
    }'
```

## Configuration Options
```
{
    "MaxNumberOfConcurrentMessages": 10,    // Maximum number of messages to process concurrently
    "VisibilityTimeout": 20,               // Time (in seconds) that a message is invisible after being received
    "WaitTimeSeconds": 20,                 // Long polling duration
    "VisibilityTimeoutExtensionHeartbeatInterval": 1,  // How often to extend visibility timeout
    "VisibilityTimeoutExtensionThreshold": 5   // When to extend visibility timeout
}
```
### Backoff Policy Options
```
{
    "BackoffPolicy": "CappedExponential",  // Available options: Linear, Exponential, CappedExponential
    "CappedExponentialBackoffOptions": {
        "CapBackoffTime": 60               // Maximum backoff time in seconds
    }
}
```

