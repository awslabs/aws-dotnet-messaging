// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace SubscriberApp
{
    public class Program
    {
        private const string QUEUE_URL = "QUEUE_URL";

        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddAWSMessageBus(bus =>
                    {
                        bus.AddMessageHandler<TransactionHandler, TransactionInfo>("transactionInfo");
                        bus.AddSQSPoller(QUEUE_URL, options =>
                        {
                            options.MaxNumberOfConcurrentMessages = 5;
                        });
                    });
                })
                .Build();

            host.Run();
        }
    }
}
