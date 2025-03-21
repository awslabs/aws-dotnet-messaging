# AWS Message Processing Framework with Polly Integration

This sample application demonstrates how to use the AWS Message Processing Framework for .NET with Polly for resilient message processing. It showcases integration between SQS message processing and Polly's retry policies.

## Overview

This sample demonstrates:
- Integration of AWS Message Processing Framework with Polly for resilient messaging
- Custom backoff handling for message processing
- SQS message processing with typed handlers
- OpenTelemetry integration for observability
- Both code-based and configuration-based setup options

## Prerequisites

- .NET 8.0 or later
- AWS Account with appropriate permissions
- Basic understanding of Amazon SQS and AWS Message Processing Framework

## Setup

### 1. Create an SQS Queue

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
builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MPF"); // Replace with your Queue URL
builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");
```
And keep this line commented:
```
// builder.LoadConfigurationFromSettings(context.Configuration);
```

#### Option B: Configuration-based (appsettings.json)
1. Comment out the code-based configuration in Program.cs:

```
// builder.AddSQSPoller("https://sqs.us-west-2.amazonaws.com/012345678910/MPF");
// builder.AddMessageHandler<ChatMessageHandler, ChatMessage>("chatMessage");
```
2. Uncomment the configuration loading:

```
builder.LoadConfigurationFromSettings(context.Configuration);
```
3. Update appsettings.json:

```
{
    "AWS.Messaging": {
        "SQSPollers": [
            {
                "QueueUrl": "https://sqs.us-west-2.amazonaws.com/012345678910/MPF"  // Replace with your Queue URL
            }
        ],
        "MessageHandlers": [
            {
                "HandlerType": "PollyIntegration.MessageHandlers.ChatMessageHandler",
                "MessageType": "PollyIntegration.Models.ChatMessage",
                "MessageTypeIdentifier": "chatMessage"
            }
        ]
    }
}

```

Choose Option A if you:

- Want configuration close to the code
    
- Need dynamic runtime configuration
    
- Are prototyping or testing
    

Choose Option B if you:

- Need configuration changes without recompiling
    
- Want environment-specific settings
    
- Prefer separation of configuration from code
    
- Need to manage multiple configurations
    

### 3. Configure AWS Credentials

Ensure you have AWS credentials configured either through:

- AWS CLI ( 
    
    ```plaintext
    aws configure
    ```
    
    )
    
- Environment variables
    
- AWS credentials file
    
- IAM role (if running on AWS)

## Project Structure
PollyIntegration/
├── MessageHandlers/
│   └── ChatMessageHandler.cs    # Sample message handler
├── Models/
│   └── ChatMessage.cs          # Message type definition
├── Program.cs                  # Application entry point
├── PollyBackoffHandler.cs      # Custom Polly integration
└── appsettings.json           # Application configuration

## Running  the Application
1. Build the project
```
dotnet build
```
2. Run the application
```
dotnet run
```
## Testing

### Send a Test Message

You can send a test message to your SQS queue using the AWS Console or AWS CLI:

Using AWS CLI:
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
Replace YOUR_QUEUE_URL with your actual SQS queue URL.

## Additional Configuration Options

### Polly Backoff Configuration
```
{
    "AWS.Messaging": {
        "BackoffPolicy": "CappedExponential",
        "CappedExponentialBackoffOptions": {
            "CapBackoffTime": 2
        }
    }
}

```
