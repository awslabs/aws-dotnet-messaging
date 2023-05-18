// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Util;

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around <see cref="EC2InstanceMetadata"/>.
/// </summary>
internal interface IEC2InstanceMetadataManager
{
    /// <summary>
    /// The ID of the EC2 instance.
    /// </summary>
    string InstanceId { get; }
}
