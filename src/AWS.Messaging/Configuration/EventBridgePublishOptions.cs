// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Contains additional properties that can be set while configuring a EventBridge publisher.
/// </summary>
public class EventBridgePublishOptions
{
    /// <summary>
    /// The ID of the global EventBridge endpoint.
    /// </summary>
    public string? EndpointID { get; set; }
}
