// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging.Services.Backoff.Policies;

/// <summary>
/// A backoff policy that will not perform a backoff.
/// This policy can essentially be used to disable the backoff logic of the <see cref="IBackoffHandler"/>.
/// </summary>
internal class NoBackoffPolicy : IBackoffPolicy
{
    /// <summary>
    /// This is a no-op and will always return false indicating that a backoff should not occur.
    /// </summary>
    /// <returns>false</returns>
    public bool ShouldBackoff(Exception exception, SQSMessagePollerConfiguration configuration) => false;

    /// <summary>
    /// This is a no-op and will always return 0 as the backoff time is not needed.
    /// </summary>
    /// <returns>0</returns>
    public TimeSpan RetrieveBackoffTime(int numberOfRetries) => TimeSpan.Zero;
}
