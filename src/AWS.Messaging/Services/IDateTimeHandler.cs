// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// Interface that exposes methods dealing with DateTime
/// </summary>
public interface IDateTimeHandler
{
    /// <summary>
    /// Returns the current DateTime in UTC
    /// </summary>
    DateTimeOffset GetUtcNow();
}
