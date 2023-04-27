// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;

namespace AWS.Messaging;

/// <summary>
/// A wrapper for the exceptions thrown by AWS Messaging.
/// </summary>
public abstract class AWSMessagingException : Exception
{
    /// <summary>
    /// Creates an instance of <see cref="AWSMessagingException"/>.
    /// </summary>
    public AWSMessagingException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if the message type full name cannot be retrieved.
/// </summary>
public class InvalidMessageTypeException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidMessageTypeException"/>.
    /// </summary>
    public InvalidMessageTypeException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if the publisher endpoint is not valid.
/// </summary>
public class InvalidPublisherEndpointException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidPublisherEndpointException"/>.
    /// </summary>
    public InvalidPublisherEndpointException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// An exception thrown with the configuration for messaging is invalid.
/// </summary>
public class ConfigurationException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="AWS.Messaging.ConfigurationException" />
    /// </summary>
    public ConfigurationException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if the publisher type full name cannot be retrieved.
/// </summary>
public class InvalidPublisherTypeException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidPublisherTypeException"/>.
    /// </summary>
    public InvalidPublisherTypeException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if the a publisher mapping cannot be found for a specific message type.
/// </summary>
public class MissingMessageTypeConfigurationException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="MissingMessageTypeConfigurationException"/>.
    /// </summary>
    public MissingMessageTypeConfigurationException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if a publisher type outside of <see cref="PublisherTargetType"/> is provided.
/// </summary>
public class UnsupportedPublisherException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="UnsupportedPublisherException"/>.
    /// </summary>
    public UnsupportedPublisherException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if the subscriber endpoint is not valid.
/// </summary>
public class InvalidSubscriberEndpointException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidSubscriberEndpointException"/>.
    /// </summary>
    public InvalidSubscriberEndpointException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if failed to deserialize the application message while invoking <see cref="IMessageSerializer.Deserialize(string, Type)"/>
/// </summary>
public class FailedToDeserializeApplicationMessageException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="FailedToDeserializeApplicationMessageException"/>.
    /// </summary>
    public FailedToDeserializeApplicationMessageException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if failed to serialize the application message while invoking <see cref="IMessageSerializer.Serialize(object)"/>
/// </summary>
public class FailedToSerializeApplicationMessageException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="FailedToSerializeApplicationMessageException"/>.
    /// </summary>
    public FailedToSerializeApplicationMessageException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if the message being sent is invalid.
/// </summary>
public class InvalidMessageException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidMessageException"/>.
    /// </summary>
    public InvalidMessageException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if failed to create a <see cref="MessageEnvelope"/>
/// </summary>
public class FailedToCreateMessageEnvelopeException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="FailedToCreateMessageEnvelopeException"/>
    /// </summary>
    public FailedToCreateMessageEnvelopeException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if failed to serialize a <see cref="MessageEnvelope"/>
/// </summary>
public class FailedToSerializeMessageEnvelopeException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="FailedToSerializeMessageEnvelopeException"/>
    /// </summary>
    public FailedToSerializeMessageEnvelopeException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if failed to create a <see cref="MessageEnvelopeConfiguration"/> instance.
/// </summary>
public class FailedToCreateMessageEnvelopeConfigurationException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="FailedToCreateMessageEnvelopeConfigurationException"/>
    /// </summary>
    public FailedToCreateMessageEnvelopeConfigurationException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown when attempting to perform an SQS operation on a message without a valid receipt handle in the <see cref="MessageEnvelope.SQSMetadata"/>
/// </summary>
public class MissingSQSReceiptHandleException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="MissingSQSReceiptHandleException"/>.
    /// </summary>
    public MissingSQSReceiptHandleException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown when an <see cref="SQSMessagePollerOptions" /> is configured with one or more invalid values
/// </summary>
public class InvalidSQSMessagePollerOptionsException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidSQSMessagePollerOptionsException"/>.
    /// </summary>
    public InvalidSQSMessagePollerOptionsException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown during message handling if unable to find an <see cref="IMessageHandler{T}.HandleAsync(MessageEnvelope{T}, CancellationToken)"/>
/// method on the handler type for a given message
/// </summary>
public class InvalidMessageHandlerSignatureException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidMessageHandlerSignatureException"/>.
    /// </summary>
    public InvalidMessageHandlerSignatureException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown if could not retrieve the AWS service client from the DI container.
/// </summary>
public class FailedToFindAWSServiceClientException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidMessageHandlerSignatureException"/>.
    /// </summary>
    public FailedToFindAWSServiceClientException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// Thrown when an <see cref="LambdaMessagePollerOptions" /> is configured with one or more invalid values
/// </summary>
public class InvalidLambdaMessagePollerOptionsException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidLambdaMessagePollerOptionsException"/>.
    /// </summary>
    public InvalidLambdaMessagePollerOptionsException(string message, Exception? innerException = null) : base(message, innerException) { }
}
