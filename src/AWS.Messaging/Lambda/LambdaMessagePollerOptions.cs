// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Lambda;

/// <summary>
/// Configuration options for polling and processing messages from SQS.
/// </summary>
public class LambdaMessagePollerOptions
{
    /// <summary>
    /// The maximum number of messages from the SQS event batch to process concurrently.
    /// </summary>
    /// <remarks>The default value is 10.</remarks>
    public int MaxNumberOfConcurrentMessages { get; init; } = 10;

    /// <summary>
    /// Validates that the options are valid against the message framework's and/or SQS limits
    /// </summary>
    /// <exception cref="InvalidSQSMessagePollerOptionsException">Thrown when one or more invalid options are found</exception>
    internal void Validate()
    {
        var errorMessages = new List<string>();

        if (MaxNumberOfConcurrentMessages <= 0)
        {
            errorMessages.Add($"{nameof(MaxNumberOfConcurrentMessages)} must be greater than 0. Current value: {MaxNumberOfConcurrentMessages}.");
        }

        if (errorMessages.Any())
        {
            throw new InvalidLambdaMessagePollerOptionsException(string.Join(Environment.NewLine, errorMessages));
        }
    }
}
