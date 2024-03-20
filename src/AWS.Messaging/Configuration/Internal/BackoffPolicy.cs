// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Services.Backoff;
using AWS.Messaging.Services.Backoff.Policies;

namespace AWS.Messaging.Configuration.Internal;

/// <summary>
/// Represents the available backoff policies that can be paired with <see cref="BackoffHandler"/>.
/// </summary>
public enum BackoffPolicy
{
    /// <summary>
    /// Represents the backoff policy <see cref="NoBackoffPolicy"/>.
    /// </summary>
    None,
    /// <summary>
    /// Represents the backoff policy <see cref="IntervalBackoffPolicy"/>.
    /// </summary>
    Interval,
    /// <summary>
    /// Represents the backoff policy <see cref="CappedExponentialBackoffPolicy"/>.
    /// </summary>
    CappedExponential
}
