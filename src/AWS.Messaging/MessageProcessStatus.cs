// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.Messaging;

/// <summary>
/// Status returned after processing messages from the <see cref="AWS.Messaging.IMessageHandler{T}"/>.
/// The status informs the <see cref="AWS.Messaging.Services.IMessageManager"/> what should be done with the message
/// after the message has been attempted to be processed.
///
/// For example using the <see cref="MessageProcessStatus.Success()"/> would tell the <see cref="AWS.Messaging.Services.IMessageManager"/> to delete
/// the message from the underlying service like SQS.
/// </summary>
public sealed class MessageProcessStatus
{
    // A class is used instead of an enum to represent status for possible future features of returning
    // a status with data. For example if we add a "DelayRetry" status that would require extra data
    // for how long the delay should happen.

    private enum StatusType { Success, Failed }

    private readonly StatusType _statusType;

    private static readonly MessageProcessStatus _success = new MessageProcessStatus(StatusType.Success);
    private static readonly MessageProcessStatus _failed = new MessageProcessStatus(StatusType.Failed);

    private MessageProcessStatus(StatusType statusType)
    {
        _statusType = statusType;
    }

    /// <summary>
    /// Creates a success status return
    /// </summary>
    /// <returns></returns>
    public static MessageProcessStatus Success() => _success;

    /// <summary>
    /// Creates a failed status return
    /// </summary>
    /// <returns></returns>
    public static MessageProcessStatus Failed() => _failed;

    /// <summary>
    /// Returns true if the status is success.
    /// </summary>
    public bool IsSuccess => _statusType == StatusType.Success;

    /// <summary>
    /// Returns true if the status is failed.
    /// </summary>
    public bool IsFailed => _statusType == StatusType.Failed;
}
