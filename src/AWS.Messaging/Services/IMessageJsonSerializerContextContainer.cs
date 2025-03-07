// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace AWS.Messaging.Services;

/// <summary>
/// Container for the JsonSerializerContext provided by users of the library. The JsonSerializerContext is
/// used when ever serializing/deserializing any of the .NET types consumers use to represent the messages.
/// </summary>
public interface IMessageJsonSerializerContextContainer
{
    /// <summary>
    /// Returns the consumer provided JsonSerializerContext.
    /// </summary>
    /// <returns></returns>
    JsonSerializerContext? GetJsonSerializerContext();
}

public class NullMessageJsonSerializerContextContainer : IMessageJsonSerializerContextContainer
{
    /// <inheritdoc/>
    public JsonSerializerContext? GetJsonSerializerContext() => null;
}

/// <summary>
/// The default implementation of IMessageJsonSerializerContextContainer when a user provides the library
/// a JsonSerializerContext to use for serializing/deserializing their types.
/// </summary>
public class DefaultMessageJsonSerializerContextContainer : IMessageJsonSerializerContextContainer
{
    private readonly JsonSerializerContext _jsonSerializerContext;

    /// <summary>
    /// Create instance holding on to the JsonSerializerContext
    /// </summary>
    /// <param name="jsonSerializerContext">The user provided JsonSerializerContext.</param>
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
