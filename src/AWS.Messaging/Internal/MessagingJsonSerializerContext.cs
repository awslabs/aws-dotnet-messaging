// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Messaging.Configuration.Internal;

namespace AWS.Messaging.Internal;

/// <summary>
/// The JsonSerializerContext used for any JSON serialization of types known by this library. The type is public
/// due to constraints of the source generator for JsonSerializerContext.
/// </summary>
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Amazon.SQS.Model.MessageAttributeValue), TypeInfoPropertyName = "SQSMessageAttributeValue")]
[JsonSerializable(typeof(Dictionary<string,Amazon.SQS.Model.MessageAttributeValue>), TypeInfoPropertyName = "DictionarySQSMessageAttributeValue")]
[JsonSerializable(typeof(Amazon.SimpleNotificationService.Model.MessageAttributeValue), TypeInfoPropertyName = "SNSMessageAttributeValue")]
[JsonSerializable(typeof(Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>), TypeInfoPropertyName = "DictionarySNSMessageAttributeValue")]
[JsonSerializable(typeof(MessageEnvelope<string>))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(TaskMetadataResponse))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
public partial class MessagingJsonSerializerContext : JsonSerializerContext
{
}
