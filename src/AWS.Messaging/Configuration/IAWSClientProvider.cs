// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Runtime;

namespace AWS.Messaging.Configuration;

/// <summary>
/// Provides an AWS service client from the DI container
/// </summary>
public interface IAWSClientProvider
{
    /// <summary>
    /// Returns the AWS service client that was injected into the DI container
    /// </summary>
    /// <typeparam name="T">This type must implement <see cref="IAmazonService"/></typeparam>
    T GetServiceClient<T>() where T : IAmazonService;
}
