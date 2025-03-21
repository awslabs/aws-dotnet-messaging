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

3. Get the test tool version:

```
dotnet lambda-test-tool info
```

4. Run the `LambdaMessaging` project. 

There are 2 ways to run it 
1. Visual studio (easiest way).
    1a. Update `Properties/launchSettings.json` and replace `${VERSIOMN} with the actual test tool version.
2. Via command line

```
cd bin\Debug\net8.0
$env:AWS_LAMBDA_RUNTIME_API = "localhost:5050/MyFunction"
$env:VERSION = "0.9.1" // Use the version returned from dotnet lambda-test-tool info

dotnet exec --depsfile ./LambdaMessaging.deps.json --runtimeconfig ./LambdaMessaging.runtimeconfig.json "$env:USERPROFILE\.dotnet\tools\.store\amazon.lambda.testtool\$env:VERSION\amazon.lambda.testtool\$env:VERSION\content\Amazon.Lambda.RuntimeSupport\net8.0\Amazon.Lambda.RuntimeSupport.dll" LambdaMessaging::LambdaMessaging.Function_FunctionHandler_Generated::FunctionHandler


```

5. You should now see the `MyFunction` appear in the test tools function list drop down in the top right corner. Select `MyFunction`.

6. We have provided a `HandlerSampleRequest.json` file to be used to test this function. Copy and paste this json into the test tools input window and then hit the "invoke button".

7. You should see in the console window that the `ChatMessageHandler` successfully processed the message. There should be a log statement saying `Message Description: Testing!!!`.
