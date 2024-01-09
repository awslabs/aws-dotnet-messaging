// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Benchmarks;

/// <summary>
/// Handler for benchmarking messages
/// </summary>
public class BenchmarkMessageHandler : IMessageHandler<BenchmarkMessage>
{
    private readonly IBenchmarkCollector _benchmarkCollector;

    public BenchmarkMessageHandler(IBenchmarkCollector benchmarkCollector)
    {
        _benchmarkCollector = benchmarkCollector;
    }

    /// <inheritdoc/>
    public Task<MessageProcessStatus> HandleAsync(MessageEnvelope<BenchmarkMessage> messageEnvelope, CancellationToken token = default)
    {
        _benchmarkCollector.RecordMessageReception(messageEnvelope.Message);

        return Task.FromResult(MessageProcessStatus.Success());
    }
}
