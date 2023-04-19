// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS.Model;

namespace AWS.Messaging.Configuration;

/// <summary>
/// Internal configuration for polling messages from SQS
/// </summary>
internal class SQSMessagePollerConfiguration : IMessagePollerConfiguration
{
    /// <summary>
    /// Default value for <see cref="MaxNumberOfConcurrentMessages"/>
    /// </summary>
    /// <remarks>The default value is 10 messages.</remarks>
    public const int DEFAULT_MAX_NUMBER_OF_CONCURRENT_MESSAGES = 10;

    /// <summary>
    /// Default value for <see cref="VisibilityTimeout"/>
    /// </summary>
    /// <remarks>The default value is 20 seconds.</remarks>
    public const int DEFAULT_VISIBILITY_TIMEOUT_SECONDS = 20;

    /// <summary>
    /// Default value for <see cref="VisibilityTimeoutExtensionInterval"/>
    /// </summary>
    /// <remarks>The default value is 18 seconds.</remarks>
    public const int DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_INTERVAL_SECONDS = 18;

    /// <summary>
    /// Default value for <see cref="WaitTimeSeconds"/>
    /// </summary>
    /// <remarks>The default value is 20 seconds.</remarks>
    public const int DEFAULT_WAIT_TIME_SECONDS = 20;

    /// <summary>
    /// The SQS QueueUrl to poll messages from.
    /// </summary>
    public string SubscriberEndpoint { get; }

    /// <summary>
    /// The maximum number of messages from this queue to process concurrently.
    /// </summary>
    /// <remarks><inheritdoc cref="DEFAULT_MAX_NUMBER_OF_CONCURRENT_MESSAGES" path="//remarks"/></remarks>
    public int MaxNumberOfConcurrentMessages { get; init; } = DEFAULT_MAX_NUMBER_OF_CONCURRENT_MESSAGES;

    /// <summary>
    /// <inheritdoc cref="ReceiveMessageRequest.VisibilityTimeout"/>
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="DEFAULT_VISIBILITY_TIMEOUT_SECONDS" path="//remarks"/>
    /// The minimum is 0 seconds. The maximum is 12 hours.
    /// </remarks>
    public int VisibilityTimeout { get; init; } = DEFAULT_VISIBILITY_TIMEOUT_SECONDS;

    /// <summary>
    /// How often in seconds to extend the visibility timeout of messages that have been
    /// received from SQS but are still being processed
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_INTERVAL_SECONDS" path="//remarks"/>
    /// </remarks>
    public int VisibilityTimeoutExtensionInterval { get; init; } = DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_INTERVAL_SECONDS;

    /// <summary>
    /// <inheritdoc cref="ReceiveMessageRequest.WaitTimeSeconds"/>
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="DEFAULT_WAIT_TIME_SECONDS" path="//remarks"/>
    /// The minimum is 0 seconds. The maximum is 20 seconds.
    /// </remarks>
    public int WaitTimeSeconds { get; init; } = DEFAULT_WAIT_TIME_SECONDS;

    /// <summary>
    /// Construct an instance of <see cref="SQSMessagePollerConfiguration" />
    /// </summary>
    /// <param name="queueUrl">The SQS QueueUrl to poll messages from.</param>
    public SQSMessagePollerConfiguration(string queueUrl)
    {
        if (string.IsNullOrEmpty(queueUrl))
            throw new InvalidSubscriberEndpointException("The SQS Queue URL cannot be empty.");

        SubscriberEndpoint = queueUrl;
    }
}
