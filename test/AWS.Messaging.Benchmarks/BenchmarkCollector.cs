// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics;

namespace AWS.Messaging.Benchmarks;

/// <summary>
/// Aggregates the data for each message that is published and received during a benchmark run
/// </summary>
/// <remarks>
/// Inspired by a similar technique in https://github.com/MassTransit/MassTransit-Benchmark/ and
/// https://github.com/justeattakeaway/JustSaying/tree/main/tests/JustSaying.Benchmark
/// </remarks>
public interface IBenchmarkCollector
{
    /// <summary>
    /// Records the publishing of a single message
    /// </summary>
    /// <param name="publishDuration">How long the message took to publish</param>
    void RecordMessagePublish(TimeSpan publishDuration);

    /// <summary>
    /// Records the handling of a single message
    /// </summary>
    /// <param name="message">Received message</param>
    void RecordMessageReception(BenchmarkMessage message);

    /// <summary>
    /// Task that completes when the expected number of messages have been handled
    /// </summary>
    Task<TimeSpan> HandlingCompleted { get; }

    /// <summary>
    /// Publish times for all messages, in milliseconds
    /// </summary>
    List<double> PublishTimes { get; }

    /// <summary>
    /// Handling times for all messages, in milliseconds
    /// </summary>
    List<double> ReceptionTimes { get; }
}

public class BenchmarkCollector : IBenchmarkCollector
{
    private readonly ConcurrentBag<TimeSpan> _publishDurations = new();
    private readonly ConcurrentBag<TimeSpan> _receiveDurations = new();
    private readonly int _expectedNumberOfMessages;
    readonly TaskCompletionSource<TimeSpan> _receivingCompleted;
    readonly Stopwatch _stopwatch;

    public BenchmarkCollector(int expectedNumberOfMessages)
    {
        _expectedNumberOfMessages = expectedNumberOfMessages;
        _receivingCompleted = new TaskCompletionSource<TimeSpan>();

        _stopwatch = Stopwatch.StartNew();
    }

    /// <inheritdoc/>
    public void RecordMessagePublish(TimeSpan publishDuration)
    {
        _publishDurations.Add(publishDuration);
    }

    /// <inheritdoc/>
    public void RecordMessageReception(BenchmarkMessage message)
    {
        _receiveDurations.Add(DateTime.UtcNow - message.SentTime);

        if (_receiveDurations.Count == _expectedNumberOfMessages)
        {
            _receivingCompleted.TrySetResult(_stopwatch.Elapsed);
        }
    }

    /// <inheritdoc/>
    public Task<TimeSpan> HandlingCompleted => _receivingCompleted.Task;

    /// <inheritdoc/>
    public List<double> PublishTimes => _publishDurations.Select(x => x.TotalMilliseconds).ToList();

    /// <inheritdoc/>
    public List<double> ReceptionTimes => _receiveDurations.Select(x => x.TotalMilliseconds).ToList();
}
