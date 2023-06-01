// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;

namespace AWS.Messaging.IntegrationTests.Models
{
    public class SimulatedMessage
    {
        public string? Id { get; set; }

        public bool ReturnFailedStatus { get; set; } = false;

        public TimeSpan WaitTime { get; set; }
    }
}
