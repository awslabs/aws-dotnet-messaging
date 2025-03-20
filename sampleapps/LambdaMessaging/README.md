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


## Project Structure

```
LambdaMessaging/
├── Function.cs # Lambda function handler
├── Startup.cs # DI and service configuration
├── ChatMessage.cs # Message type definition
├── ChatMessageHandler.cs # Message handler implementation
├── serverless.template # AWS SAM template
├── LambdaMessaging.csproj # Project file
```


## Getting Started

In order to test the lambda function locally with the messaging processing framework, it requires installing the Lambda Test Tool https://github.com/aws/aws-lambda-dotnet/blob/master/Tools/LambdaTestTool-v2 first.

1. Install the AWS Lambda Test Tool:
```bash
dotnet tool install -g amazon.lambda.testtool
```

2. Start the Lambda Test Tool:

```bash
dotnet lambda-test-tool start --lambda-emulator-port 5050
```

3. Update the `Properties/launchSettings.json` file to contain the test tools's current version. You can determine the test tool's version by running `dotnet lambda-test-tool info`.

4. Run the `LambdaMessaging` project. // TODO come back to this

5. You should now see the `MyFunction` appear in the test tools function list drop down in the top right corner. Select `MyFunction`.

6. We have provided a `HandlerSampleRequest.json` file to be used to test this function. Copy and paste this json into the test tools input window and then hit the "invoke button".

7. You should see in the console window that the `ChatMessageHandler` successfully processed the message. There should be a log statement saying `Message Description: Testing!!!`.
