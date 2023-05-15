// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Lambda;

/// <summary>
/// Configuration options for polling and processing messages from SQS.
/// </summary>
public class LambdaMessagingOptions
{
    /// <summary>
    /// The default value for the <see cref="MaxNumberOfConcurrentMessages"/>
    /// </summary>
    public const int DEFAULT_MAX_NUMBER_OF_CONCURRENT_MESSAGES = 10;

    /// <summary>
    /// The maximum number of messages from the SQS event batch to process concurrently.
    /// </summary>
    /// <remarks>The default value is 10.</remarks>
    public int MaxNumberOfConcurrentMessages { get; set; } = DEFAULT_MAX_NUMBER_OF_CONCURRENT_MESSAGES;

    /// <summary>
    /// If true when a message has been successfully processed delete the message from the SQS queue. When not set
    /// to false the messages will be deleted by the Lambda service if all of the messages in the were successfully processed
    /// and the Lambda function returned no exceptions.
    ///
    /// For Lambda functions that are configured for partial failure and return an SQSBatchResponse this property is ignored.
    /// </summary>
    public bool DeleteMessagesWhenCompleted { get; set; } = false;

    /// <summary>
    /// Validates that the options are valid against the message framework's and/or SQS limits
    /// </summary>
    /// <exception cref="InvalidLambdaMessagingOptionsException">Thrown when one or more invalid options are found</exception>
    internal void Validate()
    {
        var errorMessages = new List<string>();

        if (MaxNumberOfConcurrentMessages <= 0)
        {
            errorMessages.Add($"{nameof(MaxNumberOfConcurrentMessages)} must be greater than 0. Current value: {MaxNumberOfConcurrentMessages}.");
        }

        if (errorMessages.Any())
        {
            throw new InvalidLambdaMessagingOptionsException(string.Join(Environment.NewLine, errorMessages));
        }
    }
}
