// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Configuration for polling messages from SQS
/// </summary>
public class LambdaMessagePollerOptions
{
    /// <inheritdoc cref="LambdaMessagePollerConfiguration.MaxNumberOfConcurrentMessages"/>
    public int MaxNumberOfConcurrentMessages { get; set; } = LambdaMessagePollerConfiguration.DEFAULT_MAX_NUMBER_OF_CONCURRENT_MESSAGES;

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
