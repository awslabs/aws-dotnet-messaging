// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// This is the default implementation of <see cref="IMessageIdGenerator"/> that is used by the framework.
/// </summary>
internal class MessageIdGenerator : IMessageIdGenerator
{
    /// <summary>
    /// Returns a new GUID represented as string.
    /// </summary>
    public ValueTask<string> GenerateIdAsync()
    {
        return ValueTask.FromResult(Guid.NewGuid().ToString());
    }
}
