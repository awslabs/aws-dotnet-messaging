// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AWS.Messaging.Tests.Common.Models;

public class TempStorage<T>
{
    public ConcurrentBag<MessageEnvelope<T>> Messages { get; set; } = new ConcurrentBag<MessageEnvelope<T>>();

    /// <summary>
    /// This dictionary stores FIFO messages according to their message group IDs
    /// </summary>
    public ConcurrentDictionary<string, List<MessageEnvelope<T>>> FifoMessages { get; } = new ConcurrentDictionary<string, List<MessageEnvelope<T>>>();
}
