# Lambda Messaging Sample Application

This sample application demonstrates how to use AWS Lambda with the AWS Message Processing Framework for .NET to process messages from SQS queues.

## Overview

This sample shows how to:
- Configure a Lambda function to process messages from SQS
- Use dependency injection with Lambda Annotations
- Handle message batch processing
- Implement partial batch failure responses
- Set up message handlers for specific message types

## Prerequisites

- .NET 8.0 or later
- Follow the setup instructions in the AppHost README to ensure all AWS resources and .NET Aspire components are properly configured

## Project Structure
```
LambdaMessaging/
├── Function.cs # Lambda function handler
├── Startup.cs # DI and service configuration
├── ChatMessage.cs # Message type definition
├── ChatMessageHandler.cs # Message handler implementation
├── LambdaMessaging.csproj # Project file
```


## Getting Started

The easiest way to run this sample is to follow the instructions in the AppHost README to get the .NET Aspire environment running. This will ensure all necessary AWS resources are created and configured properly.

1. Run the App Host Project
2. Select the `LambdaMessaging` resource.
3. This will take you to the Lambda Test Tool.
3. Use the provided HandlerSampleRequest.json file to test the function. Copy the JSON content into the Lambda Test Tool's input window and click "Invoke".
4. You should see in the console window that the ChatMessageHandler successfully processed the message. Look for the log statement saying `Message Description: Testing!!!`. This will be in the Aspire logs.