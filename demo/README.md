This directory contains 3 demo applications that use the [AWS.Messaging](https://www.nuget.org/packages/AWS.Messaging/0.1.0-beta) and the [AWS.Messaging.Lambda](https://www.nuget.org/packages/AWS.Messaging.Lambda/0.1.0-beta) NuGet packages. They serve as a starting point to showcase how to use the framework to create messaging based systems.

Please refer to the [documentation website](https://awslabs.github.io/aws-dotnet-messaging/docs/overview.html) to understand the full suite of options offered by the framework and modify the demo applications accordingly.

## Prerequisites
1. AWS CLI V2 - https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html
2. .NET 6.0 or higher - https://dotnet.microsoft.com/en-us/download/dotnet/6.0
3. AWS Extensions for .NET CLI - Use `dotnet tool install -g Amazon.Lambda.Tools` for installation

## template.yml
This is a CloudFormation template that is used to create 2 SQS queues. The demo applications can publish and process the messages via this queue. Users can modify this template to set up additional message publishing endpoints like an SNS topic or an EventBridge event bus.

SQS queues created by this template:
* `DemoQueue` - This is used to demo the standard message processing experience via the [AWS.Messaging](https://www.nuget.org/packages/AWS.Messaging/0.1.0-beta)  NuGet package.
* `LambdaSQSDemoQueue` - This is used to demo the message processing experience inside a Lambda function via the [AWS.Messaging.Lambda](https://www.nuget.org/packages/AWS.Messaging.Lambda/0.1.0-beta)

Use the following command to deploy the template as a CloudFormation stack:
 `aws cloudformation deploy --template-file "template.yml" --stack-name mpf-demo`
 
 Use the following command to delete the stack:
 `aws cloudformation delete-stack --stack-name mpf-demo`

## Standard Message Processing Experience
The `PublisherApp` and the `SubscriberApp` are .NET console applications that publish/process message via an SQS queue. They both deal with the `TransactionInfo` message type.

Steps to demo the standard experience:
1.  Replace the  `QUEUE_URL`  constant defined in  `./SubscriberApp/Program.cs`  with the actual SQS queue URL for the  `DemoQueue`
1. Open a terminal window and invoke  `dotnet run --project ./SubscriberApp`
1. Replace the  `PUBLISHER_ENDPOINT`  constant defined in  `./PublisherApp/Program.cs`  with the actual SQS queue URL for the  `DemoQueue`
1. In a new terminal window, invoke `dotnet run --project ./PublisherApp`
1. The terminal logs for the `SubscriberApp` will look like this:
```

		Processed transaction ID = 0 with amount = 86
		Processed transaction ID = 5 with amount = 569
		Processed transaction ID = 1 with amount = 75
		Processed transaction ID = 3 with amount = 973
		Processed transaction ID = 6 with amount = 218
		Processed transaction ID = 7 with amount = 505
		Processed transaction ID = 9 with amount = 500
		Processed transaction ID = 2 with amount = 727
		Processed transaction ID = 4 with amount = 298
		Processed transaction ID = 8 with amount = 968
```

## Lambda Message Processing Experience
1. In a text editor, open the `./LambdaSubscriberApp/serverless.template` and add the ARN of `LambdaSQSDemoQueue` in the  Lambda function event mapping.
2. In a terminal window, invoke `dotnet lambda deploy-serverless lambda-sqs-subscriber -rs true -pl ./LambdaSubscriberApp`
3. Replace the  `PUBLISHER_ENDPOINT`  constant defined in  `./PublisherApp/Program.cs`  with the actual SQS queue URL for the  `LambdaSQSDemoQueue`
4. Invoke `dotnet run --project ./PublisherApp`
5. Open the CloudWatch logs for `lambda-sqs-subscriber` Lambda function and inspect the logs. It should contain entries like - `Processed transaction ID = 5 with amount = 942`

