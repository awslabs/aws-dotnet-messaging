// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;

namespace AWS.Messaging;

/// <summary>
/// The message publisher allows publishing messages from application code to configured AWS services.
/// It exposes the <see cref="PublishAsync{T}(T, CancellationToken)"/> method which takes in a user-defined message
/// and looks up the corresponding <see cref="PublisherMapping"/> in order to route it to the appropriate AWS services.
/// </summary>
public class MessagePublisher : IMessagePublisher
{
    private readonly IMessageConfiguration _messageConfiguration;

    /// <summary>
    /// Creates an instance of <see cref="MessagePublisher"/>.
    /// </summary>
    public MessagePublisher(IMessageConfiguration messageConfiguration)
    {
        _messageConfiguration = messageConfiguration;
    }

    /// <summary>
    /// Accepts a user-defined message which is then published to an AWS service based on the
    /// configuration done during startup. It retrieves the <see cref="PublisherMapping"/> corresponding to the
    /// message type, which contains the routing information of the provided message.
    /// The method wraps the message in a <see cref="MessageEnvelope"/> which contains metadata
    /// that enables the proper transportation of the message throughout the framework.
    /// </summary>
    /// <param name="message">The message to be sent.</param>
    /// <param name="token">The cancellation token used to cancel the request.</param>
    public async Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        var mapping = _messageConfiguration.GetPublisherMapping(typeof(T));
        if (mapping == null)
        {
            throw new MissingMessageTypeConfigurationException($"The framework is not configured to accept messages of type '{typeof(T).FullName}'.");
        }

        switch (mapping.PublishTargetType)
        {
            case PublisherTargetType.SQS_PUBLISHER:
                // TODO
                break;
            case PublisherTargetType.SNS_PUBLISHER:
                // TODO
                break;
            case PublisherTargetType.EVENTBRIDGE_PUBLISHER:
                // TODO
                break;
            default:
                throw new UnsupportedPublisherException($"The publisher type '{mapping.PublishTargetType}' is not supported.");
        }

        await Task.CompletedTask;
    }
}
