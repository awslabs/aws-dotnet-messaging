// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Net;

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around <see cref="Dns"/>.
/// </summary>
internal interface IDnsManager
{
    /// <summary>
    /// Gets the host name of the local computer.
    /// </summary>
    /// <returns>A string that contains the host name of the local computer.</returns>
    string GetHostName();
}
