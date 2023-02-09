// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

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
    string Serialize<T>(MessageEnvelope<T> envelope);

    /// <summary>
    /// Converts the specified message object into <see cref="MessageEnvelope"/>
    /// </summary>
    /// <param name="message">The message object that will be transformed into a <see cref="MessageEnvelope"/></param>
    /// <param name="messageDataType">The .NET type of the underlying application message.</param>
    MessageEnvelope ConvertToEnvelope(object message, Type messageDataType);

    /// <summary>
    /// Converts the specified message object into <see cref="MessageEnvelope"/>
    /// </summary>
    /// <typeparam name="T">The .NET type of the underlying application message.</typeparam>
    /// <param name="message">The message object that will be transformed into a <see cref="MessageEnvelope"/></param>
    MessageEnvelope<T> ConvertToEnvelope<T>(object message)
    {
        return (MessageEnvelope<T>)ConvertToEnvelope(message, typeof(T));
    }
}
