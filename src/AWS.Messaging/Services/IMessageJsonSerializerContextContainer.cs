// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace AWS.Messaging.Services;

/// <summary>
/// Container for the <see cref="JsonSerializerContext"/> provided by users of the library. The <see cref="JsonSerializerContext"/> is
/// used when ever serializing/deserializing any of the .NET types consumers use to represent the messages.
/// </summary>
public interface IMessageJsonSerializerContextContainer
{
    /// <summary>
    /// Returns the consumer provided <see cref="JsonSerializerContext"/>.
    /// </summary>
    /// <returns></returns>
    JsonSerializerContext? GetJsonSerializerContext();
}

/// <summary>
/// Placeholder implementation of <see cref="IMessageJsonSerializerContextContainer"/> when the consumer does
/// not provide a <see cref="JsonSerializerContext"/>.
/// </summary>
public class NullMessageJsonSerializerContextContainer : IMessageJsonSerializerContextContainer
{
    /// <inheritdoc/>
    public JsonSerializerContext? GetJsonSerializerContext() => null;
}

/// <summary>
/// The default implementation of <see cref="IMessageJsonSerializerContextContainer"/> when a user provides the library
/// a <see cref="JsonSerializerContext"/> to use for serializing/deserializing their types.
/// </summary>
public class DefaultMessageJsonSerializerContextContainer : IMessageJsonSerializerContextContainer
{
    private readonly JsonSerializerContext _jsonSerializerContext;

    /// <summary>
    /// Create instance holding on to the <see cref="JsonSerializerContext"/>
    /// </summary>
    /// <param name="jsonSerializerContext">The user provided <see cref="JsonSerializerContext"/>.</param>
    public DefaultMessageJsonSerializerContextContainer(JsonSerializerContext jsonSerializerContext)
    {
        _jsonSerializerContext = jsonSerializerContext;
    }

    /// <inheritdoc/>
    public JsonSerializerContext? GetJsonSerializerContext()
    {
        return _jsonSerializerContext;
    }
}
