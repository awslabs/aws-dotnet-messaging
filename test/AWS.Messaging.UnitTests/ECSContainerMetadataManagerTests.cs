// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging.Configuration.Internal;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AWS.Messaging.UnitTests;

public class ECSContainerMetadataManagerTests
{
    private readonly Mock<IEnvironmentManager> _environmentManager = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<ILogger<ECSContainerMetadataManager>> _logger = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new(MockBehavior.Strict);

    private readonly string _taskMetadataEnvironmentVariable = "ECS_CONTAINER_METADATA_URI";

    [Fact]
    public async Task GetContainerTaskMetadata_NoEnvironmentVariableSet()
    {
        var ecsContainerMetadataManager = new ECSContainerMetadataManager(
            _environmentManager.Object,
            _httpClientFactory.Object,
            _logger.Object);

        var metadata = await ecsContainerMetadataManager.GetContainerTaskMetadata();

        Assert.Null(metadata);
    }

    [Fact]
    public async Task GetContainerTaskMetadata_InvalidUri()
    {
        _environmentManager.Setup(x => x.GetEnvironmentVariable(_taskMetadataEnvironmentVariable)).Returns("http://invalidhost.com/");

        var ecsContainerMetadataManager = new ECSContainerMetadataManager(
            _environmentManager.Object,
            _httpClientFactory.Object,
            _logger.Object);

        var metadata = await ecsContainerMetadataManager.GetContainerTaskMetadata();

        Assert.Null(metadata);
    }

    [Fact]
    public async Task GetContainerTaskMetadata_NoSuccessHttpCall()
    {
        _environmentManager.Setup(x => x.GetEnvironmentVariable(_taskMetadataEnvironmentVariable)).Returns("http://169.254.170.2/");
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);


        var ecsContainerMetadataManager = new ECSContainerMetadataManager(
            _environmentManager.Object,
            _httpClientFactory.Object,
            _logger.Object);

        var metadata = await ecsContainerMetadataManager.GetContainerTaskMetadata();

        Assert.Null(metadata);
    }

    [Fact]
    public async Task GetContainerTaskMetadata_InvalidJsonReturned()
    {
        _environmentManager.Setup(x => x.GetEnvironmentVariable(_taskMetadataEnvironmentVariable)).Returns("http://169.254.170.2/");
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Invalid content")
            });
        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);


        var ecsContainerMetadataManager = new ECSContainerMetadataManager(
            _environmentManager.Object,
            _httpClientFactory.Object,
            _logger.Object);

        var metadata = await ecsContainerMetadataManager.GetContainerTaskMetadata();

        _logger.Verify(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                It.Is<EventId>(eventId => eventId.Id == 0),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == "Unable to retrieve Task Arn from ECS container metadata."),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.Null(metadata);
    }

    [Theory]
    [InlineData("", "arn:aws:ecs:us-west-2:012345678910:task/Cluster/123abc")]
    [InlineData("cluster$", "arn:aws:ecs:us-west-2:012345678910:task/Cluster/123abc")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        "arn:aws:ecs:us-west-2:012345678910:task/Cluster/123abc")]
    [InlineData("cluster", "")]
    [InlineData("cluster", "task")]
    public async Task GetContainerTaskMetadata_ValidationErrors(string cluster, string taskArn)
    {
        _environmentManager.Setup(x => x.GetEnvironmentVariable(_taskMetadataEnvironmentVariable)).Returns("http://169.254.170.2/");
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new TaskMetadataResponse
                {
                    Cluster = cluster,
                    TaskARN = taskArn
                }))
            });
        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);


        var ecsContainerMetadataManager = new ECSContainerMetadataManager(
            _environmentManager.Object,
            _httpClientFactory.Object,
            _logger.Object);

        var metadata = await ecsContainerMetadataManager.GetContainerTaskMetadata();

        Assert.Null(metadata);
    }

    [Theory]
    [InlineData("cluster", "arn:aws:ecs:us-west-2:012345678910:task/Cluster/123abc")]
    [InlineData("arn:aws:ecs:us-west-2:012345678910:task/Cluster", "arn:aws:ecs:us-west-2:012345678910:task/Cluster/123abc")]
    public async Task GetContainerTaskMetadata_NoValidationErrors(string cluster, string taskArn)
    {
        _environmentManager.Setup(x => x.GetEnvironmentVariable(_taskMetadataEnvironmentVariable)).Returns("http://169.254.170.2/");
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new TaskMetadataResponse
                {
                    Cluster = cluster,
                    TaskARN = taskArn
                }))
            });
        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);


        var ecsContainerMetadataManager = new ECSContainerMetadataManager(
            _environmentManager.Object,
            _httpClientFactory.Object,
            _logger.Object);

        var metadata = await ecsContainerMetadataManager.GetContainerTaskMetadata();

        Assert.NotNull(metadata);
        Assert.Equal(cluster, metadata.Cluster);
        Assert.Equal(taskArn, metadata.TaskARN);
    }
}
