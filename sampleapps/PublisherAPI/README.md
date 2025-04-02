# AWS Message Processing Framework Publisher API Sample

This sample application demonstrates how to use the AWS Message Processing Framework for .NET to publish messages to different AWS messaging services (SQS, SNS, and EventBridge).

## Overview

This sample demonstrates:
- Publishing messages to SQS queues (standard and FIFO)
- Publishing messages to SNS topics (standard and FIFO)
- Publishing messages to EventBridge
- Configuration-based setup using .NET Aspire
- Handling service-specific message options

## Prerequisites

- .NET 8.0 or later
- Follow the setup instructions in the AppHost README to ensure all AWS resources and .NET Aspire components are properly configured


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


## Testing

The API includes Swagger UI for testing. When running the Aspire AppHost, access it at: https://localhost:7204/swagger (the port may be different in Aspire)


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
