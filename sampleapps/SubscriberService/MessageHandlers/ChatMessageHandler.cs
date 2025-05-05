// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda;
using Amazon.Lambda.Model;
using AWS.Messaging;
using Newtonsoft.Json;
using SubscriberService.Models;
using System.Text;

namespace SubscriberService.MessageHandlers;

public class ChatMessageHandler : IMessageHandler<ChatMessage>
{

    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ChatMessage> messageEnvelope, CancellationToken token = default)
    {
        if (messageEnvelope?.Message == null)
        {
            return MessageProcessStatus.Failed();
        }

        var message = messageEnvelope.Message;

        Console.WriteLine($"Message Description: {message.MessageDescription}");

        try
        {
            // Invoke Lambda function
            var response = await InvokeLambdaFunction(message, token);
            Console.WriteLine($"Lambda Response: {response}");

            return MessageProcessStatus.Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error invoking Lambda: {ex.Message}");
            return MessageProcessStatus.Failed();
        }
    }

    private async Task<string> InvokeLambdaFunction(ChatMessage message, CancellationToken token)
    {
        var request = new InvokeRequest
        {
            FunctionName = "mytestfunction",
            InvocationType = InvocationType.RequestResponse,
            Payload = JsonConvert.SerializeObject("test")
        };

        AmazonLambdaClient lambdaClient = new AmazonLambdaClient();

        var response = await lambdaClient.InvokeAsync(request, token);

        using var reader = new StreamReader(response.Payload);
        return await reader.ReadToEndAsync();
    }
}
