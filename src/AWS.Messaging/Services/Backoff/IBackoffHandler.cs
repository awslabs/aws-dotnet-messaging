// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging.Services.Backoff;

/// <summary>
/// Interface for a backoff handler that is responsible for performing back-offs in the event that a given delegate throws an exception.
/// The delegate will be retried after backing off for a certain period of time.
/// The <see cref="IBackoffHandler"/> could be used with a <see cref="IBackoffPolicy"/> to determine whether a backoff should occur
/// and how long to wait between back-offs.
/// </summary>
public interface IBackoffHandler
{
    /// <summary>
    /// Performs a back off in the event that a given delegate throws an exception.
    /// The delegate will be retried after backing off for a certain period of time.
    /// </summary>
    /// <param name="task">The delegate for which to perform a backoff.</param>
    /// <param name="configuration">Internal configuration for polling messages from SQS.</param>
    /// <param name="token">The cancellation token used to cancel the request.</param>
    Task<T> BackoffAsync<T>(Func<Task<T>> task, SQSMessagePollerConfiguration configuration, CancellationToken token);
}
