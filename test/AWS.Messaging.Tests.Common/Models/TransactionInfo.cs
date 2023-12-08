// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Tests.Common.Models;

public class TransactionInfo
{
    public string TransactionId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime PublishTimeStamp { get; set; }

    /// <summary>
    /// How long the handler should simulate work before returning
    /// </summary>
    public TimeSpan WaitTime { get; set; }

    /// <summary>
    /// Whether the handler should fail or succeed
    /// </summary>
    public bool ShouldFail { get; set; } = false;
}
