// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Services;

/// <summary>
/// This interface provides the functionality to compute the message source
/// based on the hosting environment.
/// </summary>
internal interface IMessageSourceHandler
{
    /// <summary>
    /// Resolves a message source depending on the compute environment that it's executing in.
    /// </summary>
    /// <returns>The computed message source.</returns>
    Task<Uri> ComputeMessageSource();
}
