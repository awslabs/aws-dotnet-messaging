// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Messaging.IntegrationTests.Models;

/// <summary>
/// Test message than can be used to drive behavior in a test handler
/// </summary>
public class SimulatedMessage
{
    public string? Id { get; set; }

    /// <summary>
    /// Whether the handler should fail or succeed
    /// </summary>
    public bool ReturnFailedStatus { get; set; } = false;

    /// <summary>
    /// How long the handler should simulate work before returning
    /// </summary>
    public TimeSpan WaitTime { get; set; }
}
