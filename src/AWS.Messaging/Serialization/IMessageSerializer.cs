// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace AWS.Messaging.Serialization;

/// <summary>
/// Supports serialization and deserialization of domain-specific application messages.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// Serializes a .NET object into a format suitable for message transport.
    /// The specific return type depends on the implementation, but must be compatible
    /// with message transport requirements (e.g., JsonNode for CloudEvents).
    /// </summary>
    /// <param name="message">The .NET object to be serialized.</param>
    /// <returns>A serialized representation of the message in a format appropriate for the messaging system.</returns>
    dynamic Serialize(object message);

    /// <summary>
    /// Deserializes a JsonElement message into the specified .NET type.
    /// </summary>
    /// <param name="message">The JsonElement containing the message to be deserialized.</param>
    /// <param name="deserializedType">The target .NET type for deserialization.</param>
    /// <returns>An instance of the specified type containing the deserialized data.</returns>
    object Deserialize(JsonElement message, Type deserializedType);
}
