// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// This interface provides the functionality to generate a unique message ID.
/// </summary>
public interface IMessageIdGenerator
{
    /// <summary>
    /// Returns a unique ID.
    /// </summary>
    ValueTask<string> GenerateIdAsync();
}
