// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services.Backoff.Policies.Options;

/// <summary>
/// Configuration for the capped exponential backoff policy.
/// </summary>
public class CappedExponentialBackoffOptions
{
    /// <summary>
    /// The backoff time in seconds to cap the exponential backoff at.
    /// The default value is 1 hour.
    /// </summary>
    public int CapBackoffTime { get; set; } = 3600;

    /// <summary>
    /// Validates that the options set for the <see cref="CappedExponentialBackoffPolicy"/>.
    /// </summary>
    /// <exception cref="InvalidBackoffOptionsException">Thrown when one or more invalid options are found</exception>
    internal void Validate()
    {
        var errorMessages = new List<string>();

        if (CapBackoffTime < 0)
        {
            errorMessages.Add($"{nameof(CapBackoffTime)} must be greater than or equal to 0. Current value: {CapBackoffTime}.");
        }

        if (errorMessages.Any())
        {
            throw new InvalidBackoffOptionsException(string.Join(Environment.NewLine, errorMessages));
        }
    }
}
