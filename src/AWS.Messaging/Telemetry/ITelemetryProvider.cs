// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Telemetry;

/// <summary>
/// Provider for recording the telemetry emitted by AWS.Messaging library.
/// </summary>
public interface ITelemetryProvider
{
    /// <summary>
    /// Start a telemetry trace.
    /// </summary>
    /// <param name="traceName"></param>
    /// <returns></returns>
    ITelemetryTrace StartTrace(string traceName);
}
