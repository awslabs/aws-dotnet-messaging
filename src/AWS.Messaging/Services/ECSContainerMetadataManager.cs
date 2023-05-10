// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Services;

/// <summary>
/// A wrapper around ECS container metadata.
/// </summary>
internal class ECSContainerMetadataManager : IECSContainerMetadataManager
{
    private readonly IEnvironmentManager _environmentManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MessageSourceHandler> _logger;

    public ECSContainerMetadataManager(
        IEnvironmentManager environmentManager,
        IHttpClientFactory httpClientFactory,
        ILogger<MessageSourceHandler> logger)
    {
        _environmentManager = environmentManager;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> GetContainerTaskMetadata()
    {
        var metadata = new Dictionary<string, object>();

        var ecsMetadataURI = _environmentManager.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI");
        if (string.IsNullOrEmpty(ecsMetadataURI))
            return metadata;

        try
        {
            var client = _httpClientFactory.CreateClient("ECSMetadataClient");

            var response = await client.GetAsync(new Uri($"{ecsMetadataURI}/task"));
            if (!response.IsSuccessStatusCode)
                return metadata;

            var taskMetadataJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(taskMetadataJson) ??
                metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to retrieve Task Arn from ECS container metadata.");
            return metadata;
        }
    }
}
