# AWS Message Processing Framework Sample Applications

This directory contains sample applications demonstrating different aspects and use cases of the AWS Message Processing Framework for .NET.

## Sample Projects

### PublisherAPI
A REST API demonstrating how to publish messages to various AWS messaging services:
- Publishing to SQS (standard and FIFO queues)
- Publishing to SNS (standard and FIFO topics)
- Publishing to EventBridge
- Configuration-based and code-based setup options

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
