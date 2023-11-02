// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Messaging.Tests.Common.Models;

public class TransactionInfo
{
    public string TransactionId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime PublishTimeStamp { get; set; }

    public TimeSpan WaitTime { get; set; }

    public bool shouldFail { get; set; } = false;
}
