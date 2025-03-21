# AWS Message Processing Framework Publisher API Sample

This sample application demonstrates how to use the AWS Message Processing Framework for .NET to publish messages to different AWS messaging services (SQS, SNS, and EventBridge).

## Overview

This sample demonstrates:
- Publishing messages to SQS queues (standard and FIFO)
- Publishing messages to SNS topics (standard and FIFO)
- Publishing messages to EventBridge
- Configuration-based and code-based setup options
- Handling service-specific message options

## Prerequisites

- .NET 8.0 or later
- AWS Account with appropriate permissions
- Basic understanding of AWS messaging services (SQS, SNS, EventBridge)

## Setup

### 1. Create AWS Resources

#### SQS Queues
1. Create a standard SQS queue named "MPF"
2. Create a FIFO SQS queue named "MPF.fifo"
3. Note the queue URLs

#### SNS Topics
1. Create a standard SNS topic named "MPF"
2. Create a FIFO SNS topic named "MPF.fifo"
3. Note the topic ARNs

#### EventBridge
1. Note your default event bus ARN or create a custom event bus

### 2. Configure Message Publishing

You can choose either configuration approach:

#### Option A: Code-based Configuration

In `Program.cs`, update the endpoints and keep these lines uncommented:
```csharp
bus.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/012345678910/MPF", "chatMessage");
bus.AddSNSPublisher<OrderInfo>("arn:aws:sns:us-west-2:012345678910:MPF", "orderInfo");
bus.AddEventBridgePublisher<FoodItem>("arn:aws:events:us-west-2:012345678910:event-bus/default", "foodItem");

// FIFO endpoints
bus.AddSQSPublisher<TransactionInfo>("https://sqs.us-west-2.amazonaws.com/012345678910/MPF.fifo", "transactionInfo");
bus.AddSNSPublisher<BidInfo>("arn:aws:sns:us-west-2:012345678910:MPF.fifo", "bidInfo");
```
And keep this line commented:

```
// bus.LoadConfigurationFromSettings(builder.Configuration);
```
#### Option B: Configuration-based (appsettings.json)

1. Comment out the code-based configuration in Program.cs
    
2. Uncomment the configuration loading:
```
bus.LoadConfigurationFromSettings(builder.Configuration);
```
3. Update appsettings.json:

```
{
    "AWS.Messaging": {
        "SQSPublishers": [
            {
                "MessageType": "PublisherAPI.Models.ChatMessage",
                "QueueUrl": "https://sqs.us-west-2.amazonaws.com/012345678910/MPF",
                "MessageTypeIdentifier": "chatMessage"
            },
            {
                "MessageType": "PublisherAPI.Models.TransactionInfo",
                "QueueUrl": "https://sqs.us-west-2.amazonaws.com/012345678910/MPF.fifo",
                "MessageTypeIdentifier": "transactionInfo"
            }
        ],
        "SNSPublishers": [
            {
                "MessageType": "PublisherAPI.Models.OrderInfo",
                "TopicUrl": "arn:aws:sns:us-west-2:012345678910:MPF",
                "MessageTypeIdentifier": "orderInfo"
            },
            {
                "MessageType": "PublisherAPI.Models.BidInfo",
                "TopicUrl": "arn:aws:sns:us-west-2:012345678910:MPF.fifo",
                "MessageTypeIdentifier": "bidInfo"
            }
        ],
        "EventBridgePublishers": [
            {
                "MessageType": "PublisherAPI.Models.FoodItem",
                "EventBusName": "arn:aws:events:us-west-2:012345678910:event-bus/default",
                "MessageTypeIdentifier": "foodItem"
            }
        ]
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
PublisherAPI/
├── Controllers/
│   └── PublisherController.cs   # API endpoints for publishing
├── Models/
│   ├── ChatMessage.cs          # Standard queue message
│   ├── TransactionInfo.cs      # FIFO queue message
│   ├── OrderInfo.cs            # Standard topic message
│   ├── BidInfo.cs             # FIFO topic message
│   └── FoodItem.cs            # EventBridge message
├── Program.cs                  # Application entry point
└── appsettings.json           # Application configuration

```

## Running the Application

1. Build the project:
    

```bash
dotnet build
```

2. Run the application:
    

```bash
dotnet run
```


## Testing
The API includes Swagger UI for testing. Access it at:

```
https://localhost:7204/swagger
```
### Example API Requests

#### Send Chat Message (Standard SQS):
```
POST /Publisher/chatmessage
Content-Type: application/json

{
    "messageDescription": "Hello World!"
}
```

#### Send Transaction (FIFO SQS):
```
POST /Publisher/transactioninfo
Content-Type: application/json

{
    "transactionId": "123"
}
```
#### Send Order (Standard SNS):
```
POST /Publisher/order
Content-Type: application/json

{
    "orderId": "456",
    "userId": "user123"
}
```
#### Send Bid (FIFO SNS):
```
POST /Publisher/bidinfo
Content-Type: application/json

{
    "bidId": "789"
}

```

#### Send Food Item (EventBridge):
```
POST /Publisher/fooditem
Content-Type: application/json

{
    "id": 1,
    "name": "Pizza"
}
```