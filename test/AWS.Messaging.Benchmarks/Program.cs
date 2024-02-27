// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.CommandLine;
using System.Diagnostics;
using Amazon.SQS;
using AWS.Messaging.Publishers.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Perfolizer.Mathematics.Histograms;
using Perfolizer.Mathematics.QuantileEstimators;

namespace AWS.Messaging.Benchmarks;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var queueOption = new Option<string>(
            name: "--queueUrl",
            description: "SQS queue URL. NOTE: it will be purged prior to executing the test.");

        var numMessagesOption = new Option<int>(
            name: "--numMessages",
            description: "Number of messages to send.",
            getDefaultValue: () => 1000);

        var publishConcurrencyOption = new Option<int>(
            name: "--publishConcurrency",
            description: "Maximum number of concurrent publishing tasks to run.",
            getDefaultValue: () => 10);

        var handlerConcurrencyOption = new Option<int>(
            name: "--handerConcurrency",
            description: "Maximum number of messages to handle concurrently.",
            getDefaultValue: () => 10);

        var publishBeforePollingOption = new Option<bool>(
            name: "--publishBeforePolling",
            description: "Whether all messages should be published prior to starting the poller.",
            getDefaultValue: () => true);

        var rootCommand = new RootCommand("AWS Message Processing Framework for .NET performance benchmarks");
        rootCommand.AddOption(queueOption);
        rootCommand.AddOption(numMessagesOption);
        rootCommand.AddOption(publishConcurrencyOption);
        rootCommand.AddOption(handlerConcurrencyOption);
        rootCommand.AddOption(publishBeforePollingOption);

        rootCommand.SetHandler(async (queueUrl, numberOfMessages, publishConcurrency, handlerConcurrency, publishBeforePolling) =>
        {
            await RunBenchmarkAsync(queueUrl, numberOfMessages, publishConcurrency, handlerConcurrency, publishBeforePolling);

        }, queueOption, numMessagesOption, publishConcurrencyOption, handlerConcurrencyOption, publishBeforePollingOption);

        var iterationsOption = new Option<int>(
            name: "--iterations",
            description: "The number of iterations of the benchmark suite to run.",
            getDefaultValue: () => 5);

        var multiCommand = new Command("multi", "Run multiple benchmark iterations");
        rootCommand.AddCommand(multiCommand);
        multiCommand.AddOption(queueOption);
        multiCommand.AddOption(numMessagesOption);
        multiCommand.AddOption(publishConcurrencyOption);
        multiCommand.AddOption(handlerConcurrencyOption);
        multiCommand.AddOption(publishBeforePollingOption);
        multiCommand.AddOption(iterationsOption);

        multiCommand.SetHandler(async (queueUrl, numberOfMessages, publishConcurrency, handlerConcurrency, publishBeforePolling, iterations) =>
        {
            await RunMultipleBenchmarksAsync(queueUrl, numberOfMessages, publishConcurrency, handlerConcurrency, publishBeforePolling, iterations);

        }, queueOption, numMessagesOption, publishConcurrencyOption, handlerConcurrencyOption, publishBeforePollingOption, iterationsOption);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Executes a single benchmark run and prints the results
    /// </summary>
    /// <param name="queueUrl">SQS queue URL</param>
    /// <param name="numberOfMessages">Number of messages to send</param>
    /// <param name="publishConcurrency">Maximum number of concurrent publishing tasks to run</param>
    /// <param name="handlerConcurrency">Maximum number of messages to handle concurrently</param>
    /// <param name="publishBeforePolling">Whether all messages should be published prior to starting the poller</param>
    /// <param name="shouldPrint">Whether the test run should print it results to console</param>
    /// <returns>Collector holding data for this benchmark run</returns>
    public static async Task<BenchmarkCollector> RunBenchmarkAsync(string queueUrl,
        int numberOfMessages, int publishConcurrency, int handlerConcurrency, bool publishBeforePolling, bool shouldPrint = true)
    {
        ArgumentNullException.ThrowIfNull(queueUrl);

        // Purge the queue before starting
        var client = new AmazonSQSClient();
        await client.PurgeQueueAsync(queueUrl);

        var benchmarkCollector = new BenchmarkCollector(numberOfMessages);

        var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Error);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IBenchmarkCollector>(benchmarkCollector);
                services.AddAWSMessageBus(builder =>
                {
                    builder.AddSQSPublisher<BenchmarkMessage>(queueUrl);
                    builder.AddMessageHandler<BenchmarkMessageHandler, BenchmarkMessage>();
                    builder.AddSQSPoller(queueUrl, options =>
                    {
                        options.MaxNumberOfConcurrentMessages = handlerConcurrency;
                    });
                });
            }).Build();

        if (shouldPrint)
        {
            Console.WriteLine("Running single poller test with: ");
            Console.WriteLine($"    Queue URL: {queueUrl}");
            Console.WriteLine($"    Number of messages: {numberOfMessages}");
            Console.WriteLine($"    Publish concurrency: {publishConcurrency}");
            Console.WriteLine($"    Handler concurrency: {handlerConcurrency}");
            Console.WriteLine(publishBeforePolling ?
                              $"    All messages will be published before starting the poller." :
                              $"    The poller will be started before publishing, so messages are handled as they are published.");
        }

        var publisher = host.Services.GetRequiredService<ISQSPublisher>();
        var cts = new CancellationTokenSource();
        TimeSpan publishElapsedTime;
        TimeSpan handlingElapsedTime;

        if (publishBeforePolling) // await the publishing of all messages, then start the poller
        {
            publishElapsedTime = await PublishMessages(publisher, benchmarkCollector, numberOfMessages, publishConcurrency);
            _ = host.StartAsync(cts.Token);
        }
        else // Start the poller first, then publish
        {
            _ = host.StartAsync(cts.Token);
            publishElapsedTime = await PublishMessages(publisher, benchmarkCollector, numberOfMessages, publishConcurrency);
        }

        // This will complete once the the expected number of messages have been handled
        handlingElapsedTime = await benchmarkCollector.HandlingCompleted;
        await host.StopAsync();

        // Print the results
        if (shouldPrint)
        {
            DisplayData(benchmarkCollector.PublishTimes, publishElapsedTime, numberOfMessages, "Publishing");
            DisplayData(benchmarkCollector.ReceptionTimes, handlingElapsedTime, numberOfMessages, "Receiving");
        }

        host.Dispose();
        return benchmarkCollector;
    }

    /// <summary>
    /// Executes a multiple benchmark runs and prints the messages handled per second for each
    /// </summary>
    /// <param name="queueUrl">SQS queue URL</param>
    /// <param name="numberOfMessages">Number of messages to send</param>
    /// <param name="publishConcurrency">Maximum number of concurrent publishing tasks to run</param>
    /// <param name="handlerConcurrency">Maximum number of messages to handle concurrently</param>
    /// <param name="publishBeforePolling">Whether all messages should be published prior to starting the poller</param>
    /// <param name="numIterations">The number of iterations of the single benchmark to run</param>
    public static async Task RunMultipleBenchmarksAsync(string queueUrl,
        int numberOfMessages, int publishConcurrency, int handlerConcurrency, bool publishBeforePolling, int numIterations)
    {
        Console.WriteLine($"Running {numIterations} iterations of the poller test with: ");
        Console.WriteLine($"    Queue URL: {queueUrl}");
        Console.WriteLine($"    Number of messages: {numberOfMessages}");
        Console.WriteLine($"    Publish concurrency: {publishConcurrency}");
        Console.WriteLine($"    Handler concurrency: {handlerConcurrency}");
        Console.WriteLine(publishBeforePolling ?
                          $"    All messages will be published before starting the poller." :
                          $"    The poller will be started before publishing, so messages are handled as they are published.");

        for (var benchmarkIteration = 1; benchmarkIteration <= numIterations; benchmarkIteration++)
        {
            var start = DateTime.UtcNow;
            var result = await RunBenchmarkAsync(queueUrl, numberOfMessages, publishConcurrency, handlerConcurrency, publishBeforePolling, false);
            Console.WriteLine($"Run #{benchmarkIteration}: {Math.Round(numberOfMessages / result.HandlingElapsedTime.TotalSeconds, 2)} msgs/second");

            // Need to ensure a ~minute has elapsed before we can purge the queue again
            var timeToWait = start + TimeSpan.FromSeconds(70) - DateTime.UtcNow;
            if (timeToWait > TimeSpan.Zero)
            {
                await Task.Delay(timeToWait);
            }
        }
    }

    /// <summary>
    /// Publishes the specified number of messages
    /// </summary>
    /// <param name="publisher">SQS publisher</param>
    /// <param name="benchmarkCollector"></param>
    /// <param name="messageCount">Number of messages to publish</param>
    /// <param name="numberOfThreads">Number of concurrent publishing tasks</param>
    /// <returns>Total elapsed time to send all messages</returns>
    private static async Task<TimeSpan> PublishMessages(ISQSPublisher publisher, IBenchmarkCollector benchmarkCollector, int messageCount, int maxDegreeOfParallelism)
    {
        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Parallel.ForEachAsync(Enumerable.Range(0, messageCount), options, async (messageNumber, token) =>
        {
            var start = stopwatch.Elapsed;
            await publisher.SendAsync(new BenchmarkMessage { SentTime = DateTime.UtcNow }, null, token);
            var publishDuration = stopwatch.Elapsed - start;

            benchmarkCollector.RecordMessagePublish(publishDuration);
        });

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Prints the data for a "phase" of the benchmark
    /// </summary>
    /// <param name="messageTimes">Times for each message, in milliseconds</param>
    /// <param name="totalElapsedTime">Total elapsed time for this phase</param>
    /// <param name="numberOfMessages">Total number of messages</param>
    /// <param name="header">Header for the section (such as "Publishing")</param>
    private static void DisplayData(List<double> messageTimes, TimeSpan totalElapsedTime, int numberOfMessages, string header)
    {
        var quartiles = Quartiles.Create(messageTimes);

        Console.WriteLine($"{header}: ");
        Console.WriteLine($"    Total  time: {totalElapsedTime:mm':'ss':'fff}");
        Console.WriteLine($"    Rate: {Math.Round(numberOfMessages / totalElapsedTime.TotalSeconds, 2)} msgs/second");
        Console.WriteLine($"    Min: {Math.Round(quartiles.Min, 2)} ms");
        Console.WriteLine($"    P25: {Math.Round(quartiles.Q1, 2)} ms");
        Console.WriteLine($"    P50: {Math.Round(quartiles.Q2, 2)} ms");
        Console.WriteLine($"    P75: {Math.Round(quartiles.Q3, 2)} ms");
        Console.WriteLine($"    P99: {Math.Round(SimpleQuantileEstimator.Instance.Quantile(new Perfolizer.Common.Sample(messageTimes), 0.99), 2)} ms");
        Console.WriteLine($"    Max: {Math.Round(quartiles.Max, 2)} ms");
        Console.WriteLine(HistogramBuilder.Simple.Build(messageTimes).ToString());
    }
}
