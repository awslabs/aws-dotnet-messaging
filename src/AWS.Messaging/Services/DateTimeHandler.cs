// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// This class contains methods that deal with dates and time. It is the default implementation of <see cref="IDateTimeHandler"/>.
/// </summary>
internal class DateTimeHandler : IDateTimeHandler
{
    /// <summary>
    /// Returns the current time in UTC as a <see cref="DateTimeOffset"/> object.
    /// </summary>
    public DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
