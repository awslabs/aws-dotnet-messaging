# AWS Message Processing Framework with Polly Integration

This sample application demonstrates how to use the AWS Message Processing Framework for .NET with Polly for resilient message processing. It showcases integration between SQS message processing and Polly's retry policies.

## Overview

This sample demonstrates:
- Integration of AWS Message Processing Framework with Polly for resilient messaging
- Custom backoff handling for message processing
- SQS message processing with typed handlers
- Configuration-based setup using .NET Aspire

## Prerequisites

- .NET 8.0 or later
- Follow the setup instructions in the AppHost README to ensure all AWS resources and .NET Aspire components are properly configured


## Project Structure
```
PollyIntegration/
├── MessageHandlers/
│   └── ChatMessageHandler.cs    # Sample message handler
├── Models/
│   └── ChatMessage.cs          # Message type definition
├── Program.cs                  # Application entry point
├── PollyBackoffHandler.cs      # Custom Polly integration
└── appsettings.json           # Application configuration
```

## Testing

You can test the application using one of these two methods:

### 1. Using PublisherAPI (Recommended)

1. Ensure the Aspire AppHost is running
2. Open the PublisherAPI Swagger UI
3. Navigate to the ChatMessage endpoint
4. Send a test message using the Swagger interface

### 2. Manual Testing via AWS CLI

You can send a test message to your SQS queue using the AWS Console or AWS CLI:

Using AWS CLI:
```

$messageBody = "{""""type"""":""""chatMessage"""",""""id"""":""""123"""",""""source"""":""""test"""",""""specversion"""":""""1.0"""",""""time"""":""""2024-01-01T00:00:00Z"""",""""data"""":""""{\\""""messageDescription\\"""":\\""""Test message\\""""}""""}"

aws sqs send-message --queue-url YOUR_QUEUE_URL --message-body $messageBody

```
Replace YOUR_QUEUE_URL with your actual SQS queue URL.
