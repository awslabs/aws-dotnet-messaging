// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around <see cref="Environment"/>.
/// </summary>
internal class EnvironmentManager : IEnvironmentManager
{
    /// <inheritdoc/>
    public string? GetEnvironmentVariable(string variable) => Environment.GetEnvironmentVariable(variable);
}
