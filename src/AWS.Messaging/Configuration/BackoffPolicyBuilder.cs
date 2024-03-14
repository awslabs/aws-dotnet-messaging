// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration.Internal;
using AWS.Messaging.Services.Backoff.Policies.Options;

namespace AWS.Messaging.Configuration;

/// <summary>
/// This builder is used to configure the backoff policy and its options.
/// </summary>
public class BackoffPolicyBuilder : IBackoffPolicyBuilder
{
    private readonly MessageConfiguration _messageConfiguration;

    /// <summary>
    /// Creates an instance of <see cref="BackoffPolicyBuilder"/>.
    /// </summary>
    public BackoffPolicyBuilder(MessageConfiguration messageConfiguration)
    {
        _messageConfiguration = messageConfiguration;
    }

    /// <inheritdoc/>
    public void UseNoBackoff()
    {
        _messageConfiguration.BackoffPolicy = BackoffPolicy.None;
    }

    /// <inheritdoc/>
    public void UseIntervalBackoff(Action<IntervalBackoffOptions>? options = null)
    {
        _messageConfiguration.BackoffPolicy = BackoffPolicy.Interval;
        options?.Invoke(_messageConfiguration.IntervalBackoffOptions);

        _messageConfiguration.IntervalBackoffOptions.Validate();
    }

    /// <inheritdoc/>
    public void UseCappedExponentialBackoff(Action<CappedExponentialBackoffOptions>? options = null)
    {
        _messageConfiguration.BackoffPolicy = BackoffPolicy.CappedExponential;
        options?.Invoke(_messageConfiguration.CappedExponentialBackoffOptions);

        _messageConfiguration.CappedExponentialBackoffOptions.Validate();
    }
}
