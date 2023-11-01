// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace AWS.Messaging.IntegrationTests.Models;

public class TempStorage<T>
{
    public ConcurrentBag<MessageEnvelope<T>> Messages { get; set; } = new ConcurrentBag<MessageEnvelope<T>>();
}
