// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS.Model;

namespace AWS.Messaging.Serialization;

/// <summary>
/// Supports serialization and deserialization of <see cref="MessageEnvelope"/> into different .NET types
/// </summary>
public interface IEnvelopeSerializer
{
    /// <summary>
    /// Serializes <see cref="MessageEnvelope{T}"/> into a string.
    /// </summary>
    /// <param name="envelope"><see cref="MessageEnvelope{T}"/></param>
    ValueTask<string> SerializeAsync<T>(MessageEnvelope<T> envelope);

    /// <summary>
    /// Creates a <see cref="MessageEnvelope{T}"/>
    /// </summary>
    /// <typeparam name="T">The .NET type of the underlying application message.</typeparam>
    /// <param name="message">The application message sent by the user</param>
    ValueTask<MessageEnvelope<T>> CreateEnvelopeAsync<T>(T message);

    /// <summary>
    /// Takes an SQS <see cref="Message"/> and converts the <see cref="Message.Body"/> into a <see cref="MessageEnvelope"/>
    /// </summary>
    /// <param name="message">The SQS <see cref="Message"/> sent by the user</param>
    ValueTask<ConvertToEnvelopeResult> ConvertToEnvelopeAsync(Message message);
}
