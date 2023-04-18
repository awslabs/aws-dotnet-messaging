// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace AWS.Messaging.IntegrationTests.Models;

public class EventBridgeEnvelope
{
    [JsonPropertyName("detail")]
    public MessageEnvelope<string> Detail { get; set; } = default!;
}
