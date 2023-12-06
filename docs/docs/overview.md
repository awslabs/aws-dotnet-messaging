# AWS Message Processing Framework for .NET

**Notice:** *This library is still in early active development and is not ready for use beyond experimentation.*

The AWS Message Processing Framework for .NET is an AWS native framework that simplifies development of .NET message processing applications using AWS services. 

The purpose of the framework is to reduce the amount of boiler-plate code developers need to write. The primary responsibilities of the framework are:

* In a publisher, the framework will handle routing the messages to the correct queue/topic/event bus. 
* In a consumer process, the framework will route the particular message type to the appropriate business logic.
* The framework will handle serializing/deserializing the message to .NET objects, keeping track of the message visibility while it is being processed, and deleting the message when completed.

# Project Status

The framework is currently under active development. 

Already done:
* Support for publishing to SQS, SNS and EventBridge
* Support for polling messages from an SQS queue
* Support for customizing serialization
* Message manager to manage message lifecycle

Features to be added:
* Polling messages from Lambda
* Performance hardening
* Improve exception handling
* Configure the framework using `IConfiguration`
* Add telemetry to track messages through the framework

# Useful Links
* [AWS Message Processing Framework for .NET Design Document](./design/message-processing-framework-design.md)

# Security

See [CONTRIBUTING](https://github.com/awslabs/aws-dotnet-messaging/blob/main/CONTRIBUTING.md) for more information.

# License

This project is licensed under the Apache-2.0 License.