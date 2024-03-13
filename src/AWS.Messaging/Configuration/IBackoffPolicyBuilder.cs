// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Services.Backoff.Policies;
using AWS.Messaging.Services.Backoff.Policies.Options;

namespace AWS.Messaging.Configuration;

/// <summary>
/// This builder interface is used to configure the backoff policy and it's options.
/// </summary>
public interface IBackoffPolicyBuilder
{
    /// <summary>
    /// Sets the backoff policy to <see cref="NoBackoffPolicy"/>, effectively disabling back-offs.
    /// </summary>
    void UseNoBackoff();

    /// <summary>
    /// Sets the backoff policy to <see cref="IntervalBackoffPolicy"/> and allows it's configuration.
    /// </summary>
    void UseIntervalBackoff(Action<IntervalBackoffOptions> options);

    /// <summary>
    /// Sets the backoff policy to <see cref="CappedExponentialBackoffPolicy"/> and allows it's configuration.
    /// </summary>
    void UseCappedExponentialBackoff(Action<CappedExponentialBackoffOptions> options);
}
