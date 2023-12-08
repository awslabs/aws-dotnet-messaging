# AWS Message Processing Framework for .NET

**Notice:** *This library is still in active development and is meant for early access and feedback purposes only. It should not be used in production environments, and any releases before 1.0.0 might include breaking changes.*

The **AWS Message Processing Framework for .NET** is an AWS-native framework that simplifies the development of .NET message processing applications that use AWS services, such as Amazon Simple Queue Service (SQS), Amazon Simple Notification Service (SNS), and Amazon EventBridge. The framework reduces the amount of boiler-plate code developers need to write, allowing you to focus on your business logic when publishing and consuming messages.
* For publishers, the framework serializes the message from a .NET object to a [CloudEvents](https://cloudevents.io/)-compatible message, and then wraps that in the service-specific AWS message. It then publishes the message to the configured SQS queue, SNS topic, or EventBridge event bus. 
* For consumers, the framework deserializes the message to its .NET object and routes it to the appropriate business logic. The framework also keeps track of the message visibility while it is being processed (to avoid processing a message more than once), and deletes the message from the queue when completed. The framework supports consuming messages in both long-running polling processes and in AWS Lambda functions.

## Project Status

The framework is currently under active development. It already supports:
* Publishing to SQS, SNS, and EventBridge
* Handling SQS messages in a long-running, polling process
* Handling SQS messages in AWS Lambda functions
* Handling messages from [FIFO (first-in-first-out) queues](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-fifo-queues.html), and respecting group ordering
* OpenTelemetry Instrumentation
* Customizing serialization

Features to be added:
* AWS X-Ray Instrumentation
* Performance and error hardening

# Getting Help
For feature requests or issues using this framework please open an [issue in this repository](https://github.com/aws/aws-dotnet-messaging/issues).

# Contributing
We welcome community contributions and pull requests. See [CONTRIBUTING.md](https://github.com/awslabs/aws-dotnet-messaging/blob/main/CONTRIBUTING.md) for information on how to submit code.

# Security
The AWS Message Processing Framework for .NET relies on the [AWS SDK for .NET](https://github.com/aws/aws-sdk-net) for communicating with AWS. Refer to the [security section](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/security.html) in the [AWS SDK for .NET Developer Guide](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/welcome.html) for more information.

If you discover a potential security issue, refer to the [security policy](https://github.com/awslabs/aws-dotnet-messaging/security/policy) for reporting information.

# Additional Resources
* [AWS Message Processing Framework for .NET Design Document](./design/message-processing-framework-design.md)
* [Sample Applications](https://github.com/awslabs/aws-dotnet-messaging/tree/main/sampleapps) in this repo contains samples of a publisher service, long-running subscriber service, and Lambda function handlers.

# License

This project is licensed under the Apache-2.0 License.
