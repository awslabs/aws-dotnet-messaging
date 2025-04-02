# AWS Message Processing Framework - Aspire AppHost

This project uses .NET Aspire to orchestrate and run the sample applications that demonstrate various aspects of the AWS Message Processing Framework for .NET. It demonstrates utilizing [integrations-on-dotnet-aspire-for-aws](https://github.com/aws/integrations-on-dotnet-aspire-for-aws/tree/main) with the Message Processing Framework.

## Overview

The AppHost project coordinates the following sample applications:
- PublisherAPI - REST API for publishing messages
- SubscriberService - Message processing service
- PollyIntegration - Resilience patterns demonstration
- LambdaMessaging - AWS Lambda integration

## Prerequisites

- .NET 8.0 or later
- AWS CLI configured with appropriate credentials
- .NET Aspire workload installed

## Project Structure
```
AppHost/
├── Program.cs # Aspire application configuration
├── app-resources.template # CloudFormation template for AWS resources
├── AppHost.csproj # Project file with Aspire references
└── appsettings.json # Application configuration
```

## AWS Resources

The application uses the following AWS resources defined in `app-resources.template`:
- Standard SQS Queue (MPF)
- FIFO SQS Queue (MPF.fifo)
- Standard SNS Topic (MPF)
- FIFO SNS Topic (MPF.fifo)
- EventBridge Event Bus (MPF-EventBus)

## Configuration

The application is configured to:
- Use AWS SDK with default profile
- Deploy to US West 2 region (configurable)
- Create required AWS resources via CloudFormation
- Configure Lambda functions with appropriate permissions

## Running the Application

1. Install .NET Aspire and prerequisites. See [here](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling?tabs=windows&pivots=visual-studio)

2. Run the App host project in Visual Studio.

This will:

* Deploy the CloudFormation stack with required AWS resources
* Start all sample applications
* Configure the necessary integrations between components

## Project References

The AppHost orchestrates the following projects:

- LambdaMessaging
    
- PollyIntegration
    
- PublisherAPI
    
- SubscriberService
    

## Important Configuration Note

The sample includes two different message processing implementations that should not run simultaneously:
- SubscriberService: Demonstrates basic message processing
- PollyIntegration: Demonstrates message processing with resilience patterns

To switch between these implementations:
1. Open `Program.cs`
2. Comment out one implementation and uncomment the other:
```csharp
// Use this for basic message processing
builder.AddProject<Projects.SubscriberService>("SubscriberService")
      .WithReference(awsResources);

// OR use this for resilience pattern demonstration
// builder.AddProject<Projects.PollyIntegration>("PollyIntegration")
//        .WithReference(awsResources);