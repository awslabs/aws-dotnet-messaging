// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Messaging.Services;

/// <inheritdoc/>
public class DefaultMessageManager : IMessageManager
{
    private readonly IMessagePoller _messagePoller;
    /// <inheritdoc/>
    public DefaultMessageManager(IMessagePoller messagePoller)
    {
        _messagePoller = messagePoller;
    }

    /// <inheritdoc/>
    public int ActiveMessageCount { get; set; }

    /// <inheritdoc/>
    public void StartProcessMessage(MessageEnvelope messageEnvelope) => throw new NotImplementedException();
}
