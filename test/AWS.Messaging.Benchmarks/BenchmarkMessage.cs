// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Benchmarks;

/// <summary>
/// Message that is sent and received during benchmarking
/// </summary>
public class BenchmarkMessage
{
    /// <summary>
    /// The date and time the message was sent in UTC
    /// </summary>
    public DateTime SentTime { get; set; }
}
