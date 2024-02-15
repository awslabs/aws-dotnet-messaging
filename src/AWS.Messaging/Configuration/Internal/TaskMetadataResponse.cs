// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration.Internal;

/// <summary>
/// Represents the tasks metadata provided by Amazon ECS container agent.
/// </summary>
public class TaskMetadataResponse
{
    /// <summary>
    /// The Amazon Resource Name (ARN) or short name of the Amazon ECS cluster to which the task belongs.
    /// </summary>
    public string Cluster { get; set; } = string.Empty;

    /// <summary>
    /// The full Amazon Resource Name (ARN) of the task to which the container belongs.
    /// </summary>
    public string TaskARN { get; set; } = string.Empty;
}
