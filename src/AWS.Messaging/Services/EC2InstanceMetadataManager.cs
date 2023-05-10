// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Util;

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around <see cref="EC2InstanceMetadata"/>.
/// </summary>
internal class EC2InstanceMetadataManager : IEC2InstanceMetadataManager
{
    /// <inheritdoc/>
    public string InstanceId
    {
        get
        {
            return EC2InstanceMetadata.InstanceId;
        }
    }
}
