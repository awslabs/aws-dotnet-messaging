// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration.Internal;

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around ECS container metadata.
/// </summary>
internal interface IECSContainerMetadataManager
{
    /// <summary>
    /// The Amazon ECS container agent injects an environment variable called ECS_CONTAINER_METADATA_URI into each container in a task.
    /// To retrieve the TaskArn related to an ECS task we can issue a GET request to ${ECS_CONTAINER_METADATA_URI}/task.
    /// </summary>
    /// <returns>Task metadata as a dictionary</returns>
    Task<TaskMetadataResponse?> GetContainerTaskMetadata();
}
