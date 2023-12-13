// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace PublisherApp;

internal class Program
{
    private const string PUBLISHER_ENDPOINT = "QUEUE_URL";

    static async Task Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddAWSMessageBus(bus =>
        {
            bus.AddSQSPublisher<TransactionInfo>(PUBLISHER_ENDPOINT, "transactionInfo");
        });

        var service = serviceCollection.BuildServiceProvider();

        var messagePublisher = service.GetRequiredService<IMessagePublisher>();

        for (var i = 0; i < 10; i++)
        {
            var transactionId = i.ToString();
            var transactionAmount = new Random().Next(1, 1001);
            await messagePublisher.PublishAsync(new TransactionInfo { TransactionId = transactionId, TransactionAmount = transactionAmount });
            await Console.Out.WriteLineAsync($"Published transaction with ID = {transactionId} with amount = {transactionAmount}");
        }
    }
}
