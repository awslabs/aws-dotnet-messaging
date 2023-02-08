// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

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
/// Thrown if the type full name cannot be retrieved.
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
