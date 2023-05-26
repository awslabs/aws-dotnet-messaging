// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Messaging.Lambda;

/// <summary>
/// Thrown when a <see cref="LambdaMessagingOptions" /> is configured with one or more invalid values
/// </summary>
public class InvalidLambdaMessagingOptionsException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="InvalidLambdaMessagingOptionsException"/>.
    /// </summary>
    public InvalidLambdaMessagingOptionsException(string message, Exception? innerException = null) : base(message, innerException) { }

}

/// <summary>
/// Thrown to communicate a fatal error in the Lambda invocation to the Lambda service.
/// </summary>
public class LambdaInvocationFailureException : AWSMessagingException
{
    /// <summary>
    /// Creates an instance of <see cref="LambdaInvocationFailureException"/>.
    /// </summary>
    public LambdaInvocationFailureException(string message, Exception? innerException = null) : base(message, innerException) { }
}
