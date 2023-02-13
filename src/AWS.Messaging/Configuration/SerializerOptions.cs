// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AWS.Messaging.Configuration;

/// <summary>
/// This class serves as a container to hold various serializer options that can control
/// the serialization/de-serialization logic of the application message.
/// </summary>
public class SerializationOptions
{
    /// <summary>
    /// This is an instance of <see cref="JsonSerializerOptions"/> that controls the serialization/de-serialization logic of the application message.
    /// </summary>
    public JsonSerializerOptions? SystemTextJsonOptions { get; set; }

}
