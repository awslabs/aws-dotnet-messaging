// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around <see cref="Environment"/>.
/// </summary>
internal interface IEnvironmentManager
{
    /// <summary>
    /// Retrieve a specified system environment variable.
    /// </summary>
    /// <param name="variable">Environment variable name</param>
    /// <returns>Environment variable value</returns>
    string? GetEnvironmentVariable(string variable);
}
