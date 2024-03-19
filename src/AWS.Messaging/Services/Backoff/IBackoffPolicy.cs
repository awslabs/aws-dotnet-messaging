// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging.Services.Backoff;

/// <summary>
/// Interface for a Backoff Policy which determines if a <see cref="IBackoffHandler"/>
/// should perform a backoff and how long to wait before performing the next backoff.
/// </summary>
public interface IBackoffPolicy
{
    /// <summary>
    /// Determines if a <see cref="IBackoffHandler"/> should perform a backoff.
    /// </summary>
    /// <param name="exception">The exception that triggered a backoff in the <see cref="IBackoffHandler"/>.</param>
    /// <param name="configuration">Internal configuration for polling messages from SQS.</param>
    /// <returns>A boolean that indicates whether the <see cref="IBackoffHandler"/> should backoff or not.</returns>
    bool ShouldBackoff(Exception exception, SQSMessagePollerConfiguration configuration);

    /// <summary>
    /// Performs a calculation based on the number of retries to determine how long the <see cref="IBackoffHandler"/>
    /// will wait before performing the next backoff.
    /// </summary>
    /// <param name="numberOfRetries">The number of times the <see cref="IBackoffHandler"/> has retried a request after performing a backoff.</param>
    /// <returns>A <see cref="TimeSpan"/> that indicates how long the <see cref="IBackoffHandler"/> should wait.</returns>
    TimeSpan RetrieveBackoffTime(int numberOfRetries);
}
