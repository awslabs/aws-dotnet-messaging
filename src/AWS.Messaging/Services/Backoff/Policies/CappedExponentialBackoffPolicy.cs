// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.Services.Backoff.Policies.Options;

namespace AWS.Messaging.Services.Backoff.Policies;

/// <summary>
/// A backoff policy that will wait an exponentially increasing time for every backoff
/// until it reaches a user-defined cap. At that point, it will switch to an interval backoff
/// with the fixed interval equal to the cap.
/// </summary>
internal class CappedExponentialBackoffPolicy : IBackoffPolicy
{
    private readonly CappedExponentialBackoffOptions _options;

    /// <summary>
    /// Constructs an instance of <see cref="CappedExponentialBackoffPolicy"/>
    /// </summary>
    /// <param name="options">Configuration for the capped exponential backoff policy.</param>
    public CappedExponentialBackoffPolicy(CappedExponentialBackoffOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Determines if <see cref="BackoffHandler"/> should perform a backoff based on the thrown exception.
    /// If the exception is a fatal one, a backoff is not performed.
    /// If the exception is not fatal, a backoff is performed.
    /// </summary>
    /// <param name="exception">The exception that triggered a backoff in <see cref="BackoffHandler"/>.</param>
    /// <param name="configuration">Internal configuration for polling messages from SQS.</param>
    /// <returns>true, if the exception is not fatal. false, if it is fatal.</returns>
    public bool ShouldBackoff(Exception exception, SQSMessagePollerConfiguration configuration)
    {
        return !configuration.IsExceptionFatal(exception);
    }

    /// <summary>
    /// Retrieves the backoff time in seconds which is exponentially increasing based on the number of retries already performed.
    /// Jitter is applied to the exponential backoff to add some amount of randomness to the backoff to spread the retries around in time.
    /// </summary>
    /// <param name="numberOfRetries">The number of times the <see cref="BackoffHandler"/> has retried a request after performing a backoff.</param>
    /// <returns>An exponentially increasing backoff time as a <see cref="TimeSpan"/>.</returns>
    public TimeSpan RetrieveBackoffTime(int numberOfRetries)
    {
        double jitter = Random.Shared.NextDouble();
        var backoffTime = Convert.ToInt32(jitter * Math.Min(Math.Pow(2, numberOfRetries), _options.CapBackoffTime));
        return TimeSpan.FromSeconds(backoffTime);
    }
}
