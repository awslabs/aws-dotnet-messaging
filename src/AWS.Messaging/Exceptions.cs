// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging;

/// <summary>
/// An exception thrown with the configuration for messaging is invalid.
/// </summary>
public class ConfigurationException : Exception
{
    /// <summary>
    /// Creates an instance of <see cref="AWS.Messaging.ConfigurationException" />
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConfigurationException(string message) : base(message) { }
}
