// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.IntegrationTests.Models;

public class SNSEnvelope
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
