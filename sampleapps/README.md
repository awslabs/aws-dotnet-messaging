# AWS Message Processing Framework Sample Applications

This directory contains sample applications demonstrating different aspects and use cases of the AWS Message Processing Framework for .NET.

## Sample Projects

### AppHost
A .NET Aspire project that orchestrates and runs all sample applications:
- Configures and deploys required AWS resources
- Manages application dependencies and connections
- Provides unified logging and monitoring
- Handles service discovery and configuration

### PublisherAPI
A REST API demonstrating how to publish messages to various AWS messaging services:
- Publishing to SQS (standard and FIFO queues)
- Publishing to SNS (standard and FIFO topics)
- Publishing to EventBridge
- Configuration-based setup options

### SubscriberService
A service application showing how to process messages from SQS queues:
- Configurable SQS message polling
- Message handler implementation
- Backoff policies
- Configuration options for concurrent processing

### PollyIntegration
Demonstrates integration with the Polly resilience library:
- Custom backoff handling
- Retry policies
- Circuit breaker patterns
- Integration with message processing

### LambdaMessaging
Shows how to use the framework with AWS Lambda:
- Lambda function configuration
- SQS event processing
- Batch message handling
- Partial batch failures
- Dependency injection with Lambda Annotations

## Getting Started

1. Follow the setup instructions in the AppHost README to configure your development environment and AWS resources.

2. Important Note: SubscriberService and PollyIntegration demonstrate different message processing implementations and should not run simultaneously. Use the AppHost's Program.cs to switch between them by commenting/uncommenting the appropriate project reference.

3. Use the PublisherAPI's Swagger interface to test message publishing to different AWS messaging services.

## Prerequisites

- .NET 8.0 or later
- AWS account with appropriate permissions
- .NET Aspire workload installed

For detailed instructions and configuration options, refer to each project's individual README file.
