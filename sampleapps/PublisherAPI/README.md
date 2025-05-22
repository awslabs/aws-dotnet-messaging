# AWS Message Processing Framework Publisher API Sample

This sample application demonstrates how to use the AWS Message Processing Framework for .NET to publish messages to an SQS queue, with AWS X-Ray tracing enabled.

## Overview

This sample demonstrates:
- Publishing messages to an SQS queue
- AWS X-Ray integration for distributed tracing
- OpenTelemetry configuration with AWS auto-instrumentation

## Prerequisites

- .NET 8.0 or later
- AWS account with SQS queue configured
- AWS X-Ray permissions

## Project Structure

```
PublisherAPI/
├── Controllers/
│   └── PublisherController.cs   # API endpoint for publishing
├── Models/
│   └── ChatMessage.cs          # Message model
├── Program.cs                  # Application entry point and configuration
└── appsettings.json           # Application settings
```

## Configuration

Update the `appsettings.json` with your SQS queue URL:

```json
{
    "AWS.Messaging": {
        "SQSPublishers": [
            {
                "MessageType": "PublisherAPI.Models.ChatMessage",
                "QueueUrl": "YOUR_SQS_QUEUE_URL",
                "MessageTypeIdentifier": "chatMessage"
            }
        ]
    }
}
```

## Testing

The API includes Swagger UI for testing. When running locally, access it at: https://localhost:7204/swagger

### Example API Request

```http
POST /Publisher/chat
Content-Type: application/json

{
    "messageDescription": "Hello World!"
}
```

## Tracing

The application is configured with AWS X-Ray tracing through OpenTelemetry. Traces will automatically appear in your AWS X-Ray console, showing the flow of messages from the API through SQS.
