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
    /// <remarks>The default value is 30 seconds.</remarks>
    public const int DEFAULT_VISIBILITY_TIMEOUT_SECONDS = 30;

    /// <summary>
    /// Default value for <see cref="VisibilityTimeoutExtensionThreshold"/>
    /// </summary>
    /// <remarks>The default value is 5 seconds.</remarks>
    public const int DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_THRESHOLD_SECONDS = 5;

    /// <summary>
    /// Default value for <see cref="VisibilityTimeoutExtensionHeartbeatInterval"/>
    /// </summary>
    /// <remarks>The default value is 1 second.</remarks>
    public static TimeSpan DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_HEARTBEAT_INTERVAL = TimeSpan.FromSeconds(1);

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
    /// When an in flight message is within this many seconds of becoming visible again, the framework will extend its visibility timeout automatically.
    /// The new visibility timeout will be set to <see cref="VisibilityTimeout"/> seconds relative to now.
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_THRESHOLD_SECONDS" path="//remarks"/>
    /// </remarks>
    public int VisibilityTimeoutExtensionThreshold { get; init; } = DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_THRESHOLD_SECONDS;

    /// <summary>
    /// How frequently the framework will check in flight messages and extend the the visibility
    /// timeout of messages that will expire within the <see cref="VisibilityTimeoutExtensionThreshold"/>.
    /// </summary>
    /// /// <remarks>
    /// <inheritdoc cref="DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_HEARTBEAT_INTERVAL" path="//remarks"/>
    /// </remarks>
    public TimeSpan VisibilityTimeoutExtensionHeartbeatInterval { get; init; } = DEFAULT_VISIBILITY_TIMEOUT_EXTENSION_HEARTBEAT_INTERVAL;

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

    /// <summary>
    /// Converts this instance to a <see cref="MessageManagerConfiguration"/>
    /// </summary>
    /// <returns></returns>
    internal MessageManagerConfiguration ToMessageManagerConfiguration()
    {
        return new MessageManagerConfiguration
        {
            VisibilityTimeoutExtensionThreshold = VisibilityTimeoutExtensionThreshold,
            VisibilityTimeoutExtensionHeartbeatInterval = VisibilityTimeoutExtensionHeartbeatInterval
        };
    }
}
