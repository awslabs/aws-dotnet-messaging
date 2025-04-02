// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Serialization;

/// <summary>
/// Supports serialization and deserialization of domain-specific application messages.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Serializes the .NET message object into a string and specifies the content type of the serialized data.
    /// </summary>
    /// <param name="message">The .NET object that will be serialized.</param>
    /// <returns>A <see cref="MessageSerializerResults"/> containing the serialized string and its content type.</returns>
    MessageSerializerResults Serialize(object message);

    /// <summary>
    /// Deserializes the raw string message into the .NET type.
    /// </summary>
    /// <param name="message">The string message that will be deserialized.</param>
    /// <param name="deserializedType">The .NET type that represents the deserialized message.</param>
    object Deserialize(string message, Type deserializedType);

    /// <summary>
    /// Deserializes the raw string message into the .NET type.
    /// </summary>
    /// <typeparam name="T">The .NET type that represents the deserialized message.</typeparam>
    /// <param name="message">The string message that will be deserialized.</param>
    T Deserialize<T>(string message)
    {
        return (T)Deserialize(message, typeof(T));
    }
}
