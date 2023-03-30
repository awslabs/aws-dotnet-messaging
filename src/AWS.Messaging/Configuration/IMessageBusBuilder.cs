// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Serialization;

namespace AWS.Messaging.Configuration;

/// <summary>
/// This builder interface is used to configure the AWS messaging framework, including adding publishers and subscribers.
/// </summary>
public interface IMessageBusBuilder
{
    /// <summary>
    /// Adds an SQS Publisher to the framework which will handle publishing
    /// the defined message type to the specified SQS queues URL.
    /// </summary>
    /// <param name="queueUrl">The SQS queue URL to publish the message to.</param>
    /// <param name="messageTypeIdentifier">The language-agnostic message type identifier. If not specified, the .NET type will be used.</param>
    IMessageBusBuilder AddSQSPublisher<TMessage>(string queueUrl, string? messageTypeIdentifier = null);

    /// <summary>
    /// Adds an SNS Publisher to the framework which will handle publishing
    /// the defined message type to the specified SNS topic URL.
    /// </summary>
    /// <param name="topicUrl">The SNS topic URL to publish the message to.</param>
    /// <param name="messageTypeIdentifier">The language-agnostic message type identifier. If not specified, the .NET type will be used.</param>
    IMessageBusBuilder AddSNSPublisher<TMessage>(string topicUrl, string? messageTypeIdentifier = null);

    /// <summary>
    /// Adds an EventBridge Publisher to the framework which will handle publishing
    /// the defined message type to the specified EventBridge event bus URL.
    /// </summary>
    /// <param name="eventBusUrl">The EventBridge event bus URL to publish the message to.</param>
    /// <param name="messageTypeIdentifier">The language-agnostic message type identifier. If not specified, the .NET type will be used.</param>
    IMessageBusBuilder AddEventBridgePublisher<TMessage>(string eventBusUrl, string? messageTypeIdentifier = null);

    /// <summary>
    /// Add a message handler for a given message type.
    /// The message handler contains the business logic of how to process a given message type.
    /// </summary>
    /// <param name="messageTypeIdentifier">The language-agnostic message type identifier. If not specified, the .NET type will be used.</param>
    IMessageBusBuilder AddMessageHandler<THandler, TMessage>(string? messageTypeIdentifier = null)
        where THandler : IMessageHandler<TMessage>;

    /// <summary>
    /// Adds an SQS queue to poll for messages.
    /// </summary>
    /// <param name="queueUrl">The SQS queue to poll for messages.</param>
    /// <param name="options">Optional configuration for polling message from SQS.</param>
    IMessageBusBuilder AddSQSPoller(string queueUrl, Action<SQSMessagePollerOptions>? options = null);

    /// <summary>
    /// Configures an instance of <see cref="SerializationOptions"/> to control the serialization/de-serialization logic for the application message.
    /// </summary>
    IMessageBusBuilder ConfigureSerializationOptions(Action<SerializationOptions> options);

    /// <summary>
    /// Adds a serialization callback that lets users inject their own metadata to incoming and outgoing messages.
    /// </summary>
    /// <param name="serializationCallback">An instance of <see cref="ISerializationCallback"/>that lets users inject their own metadata to incoming and outgoing messages.</param>
    IMessageBusBuilder AddSerializationCallback(ISerializationCallback serializationCallback);
}
