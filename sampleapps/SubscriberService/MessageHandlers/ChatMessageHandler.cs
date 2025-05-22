// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS.Messaging;
using Microsoft.Extensions.Configuration;
using SubscriberService.Models;

namespace SubscriberService.MessageHandlers;

public class ChatMessageHandler : IMessageHandler<ChatMessage>
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private readonly ILogger<ChatMessageHandler> _logger;

    public ChatMessageHandler(
        IAmazonDynamoDB dynamoDb,
        IConfiguration configuration,
        ILogger<ChatMessageHandler> logger)
    {
        _dynamoDb = dynamoDb;
        _tableName = configuration["DYNAMODB_TABLE_NAME"] 
            ?? throw new InvalidOperationException("DYNAMODB_TABLE_NAME configuration is required");
        _logger = logger;
    }

    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ChatMessage> messageEnvelope, CancellationToken token = default)
    {
        if (messageEnvelope?.Message == null)
        {
            return MessageProcessStatus.Failed();
        }

        try
        {
            var message = messageEnvelope.Message;
            _logger.LogInformation("Processing message: {MessageDescription}", message.MessageDescription);

            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new AttributeValue { S = Guid.NewGuid().ToString() },
                    ["messageDescription"] = new AttributeValue { S = message.MessageDescription },
                    ["timestamp"] = new AttributeValue { S = DateTime.UtcNow.ToString("o") }
                }
            }, token);

            _logger.LogInformation("Successfully stored message in DynamoDB");
            return MessageProcessStatus.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store message in DynamoDB");
            return MessageProcessStatus.Failed();
        }
    }
}
