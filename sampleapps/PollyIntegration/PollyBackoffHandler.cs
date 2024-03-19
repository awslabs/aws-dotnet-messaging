// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging.Configuration;
using AWS.Messaging.Services.Backoff;
using Polly;
using Polly.Registry;

namespace PollyIntegration;

public class PollyBackoffHandler : IBackoffHandler
{
    private readonly ResiliencePipelineProvider<string> _resiliencePipelineProvider;

    public PollyBackoffHandler(ResiliencePipelineProvider<string> resiliencePipelineProvider)
    {
        _resiliencePipelineProvider = resiliencePipelineProvider;
    }

    public async Task<T> BackoffAsync<T>(Func<Task<T>> task, SQSMessagePollerConfiguration configuration, CancellationToken token)
    {
        ResiliencePipeline pipeline = _resiliencePipelineProvider.GetPipeline("my-pipeline");

        // Execute the pipeline
        return await pipeline.ExecuteAsync(async cancellationToken => await task.Invoke(),
            token);
    }
}
