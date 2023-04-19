// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Configuration;

/// <summary>
/// Interface for poller configuration instances. Each type of poller will have a different set of configurations depending on the underlying service being polled from.
/// </summary>
public interface IMessagePollerConfiguration
{
    /// <summary>
    /// The AWS service endpoint to poll messages from.
    /// </summary>
    string SubscriberEndpoint { get; }
}
