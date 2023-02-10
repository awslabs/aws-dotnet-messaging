// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

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
