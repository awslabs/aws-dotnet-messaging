// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services.Backoff.Policies.Options;

/// <summary>
/// Configuration for the interval backoff policy.
/// </summary>
public class IntervalBackoffOptions
{
    /// <summary>
    /// The fixed interval in seconds to wait between back-offs.
    /// The default value is 1 second.
    /// </summary>
    public int FixedInterval { get; set; } = 1;

    /// <summary>
    /// Validates that the options set for the <see cref="IntervalBackoffPolicy"/>.
    /// </summary>
    /// <exception cref="InvalidBackoffOptionsException">Thrown when one or more invalid options are found</exception>
    internal void Validate()
    {
        var errorMessages = new List<string>();

        if (FixedInterval < 0)
        {
            errorMessages.Add($"{nameof(FixedInterval)} must be greater than or equal to 0. Current value: {FixedInterval}.");
        }

        if (errorMessages.Any())
        {
            throw new InvalidBackoffOptionsException(string.Join(Environment.NewLine, errorMessages));
        }
    }
}
