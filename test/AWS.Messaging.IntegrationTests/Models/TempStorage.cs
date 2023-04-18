// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace AWS.Messaging.IntegrationTests.Models;

public class TempStorage<T>
{
    public List<MessageEnvelope<T>> Messages { get; set; } = new List<MessageEnvelope<T>>();
}
