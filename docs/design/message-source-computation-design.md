# Message Source Computation

## Background

The AWS Message Processing framework adheres to the [CloudEvents specification](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/spec.md). It wraps each application message sent by a customer with a message envelope that contains all the required attributes specified by the CloudEvents spec. One of the required attributes is the **Source** attribute. The Source attribute provides context of where the message originated and primarily needs to be configurable at a message bus level by the user. If a source is not configured by the user, a value will be computed for it based on the environment in which the process is running.

## How to set the message source?

The **Source** attribute is stored at the `MessageConfiguration` level and will be passed to the `MessageEnvelope` irrespective of the type of message being published. When configuring the message bus, you can use the `AddMessageSource` method to set the message source.

```csharp
builder.Services.AddAWSMessageBus(builder =>
{
    // This is where the message source is set
    builder.AddMessageSource(new Uri("/fancy/backend-service", UriKind.Relative))

    // continue message bus configuration below ...
    builder.AddSQSPublisher<ChatMessage>("https://sqs.us-west-2.amazonaws.com/012345678910/TestQueue");
});
```

In case access to the `Source` attribute is needed, the attribute is accessible through the dependency injection framework by injecting `IMessageConfiguration`. The `Source` attribute is defined as follows:

```csharp
public class MessageConfiguration : IMessageConfiguration
{
    /// <summary>
    /// The relative or absolute Uri to be used as a message source.
    /// This source is added globally to any message sent through the framework.
    /// </summary>
    public Uri? Source { get; set; }
}
```

## What if I don't set the message source?

When a user does not specify any source attribute, the message processing framework will resolve a source depending on the compute environment that the process is executing in. The following compute environments are considered when computing the source.

### AWS Lambda

Lambda runtimes set several environment variables during initialization. We will look for the `AWS_LAMBDA_FUNCTION_NAME` environment variable to check if the process is running inside a Lambda function. More information on environment variables defined by AWS Lambda can be found at https://docs.aws.amazon.com/lambda/latest/dg/configuration-envvars.html#configuration-envvars-runtime

**Resolved Source** - `/AWSLambda/{FUNCTION-NAME}`

### Amazon ECS

The Amazon ECS container agent injects an environment variable called `ECS_CONTAINER_METADATA_URI` into each container in an ECS task. To retrieve the metadata related to an ECS task we can issue a GET request to `${ECS_CONTAINER_METADATA_URI}/task`. The GET request will return a JSON response representing a dictionary. The keys in this dictionary that we are interested in are the ECS `Cluster` and `TaskARN`. More information on the task metadata endpoint in Amazon ECS can be found at https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task-metadata-endpoint-v3.html.

**Resolved Source** - `/AmazonECS/{CLUSTER}/{TASK-ARN}`

### Amazon EC2

The AWS SDK for .NET exposes a utility class that represents the instance metadata in EC2 which is called `EC2InstanceMetadata`. This class contains static properties that provide access to metadata from a running EC2 instance. If this class is used on a non-EC2 instance, the properties in this class will be null. We can retrieve the EC2 instance ID using the `EC2InstanceMetadata.InstanceId` property.

**Resolved Source** - `/AmazonEC2/{INSTANCE-ID}`

### Fallback Source Attribute

If we cannot resolve the source attribute from the above compute environments then we will retrieve the DNS host name which is made available in .NET through `Dns.GetHostName()`.

**Resolved Source** - `/DNSHostName/{Dns.GetHostName()}`