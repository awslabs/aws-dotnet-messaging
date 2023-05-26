// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Net;

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around <see cref="Dns"/>.
/// </summary>
internal class DnsManager : IDnsManager
{
    /// <inheritdoc/>
    public string GetHostName() => Dns.GetHostName();
}
