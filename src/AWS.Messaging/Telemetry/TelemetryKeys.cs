// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Telemetry;

internal static class TelemetryKeys
{
    internal const string QueueUrl = "aws.messaging.sqs.queueurl";
    internal const string ObjectType = "aws.messaging.objectType";
    internal const string MessageType = "aws.messaging.messageType";
    internal const string MessageId = "aws.messaging.messageId";
    internal const string PublishTargetType = "aws.messaging.publishTargetType";
    internal const string HandlerType = "aws.messaging.handlerType";
}