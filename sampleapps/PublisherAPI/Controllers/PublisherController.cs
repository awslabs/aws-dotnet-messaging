// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging;
using Microsoft.AspNetCore.Mvc;
using PublisherAPI.Models;

namespace PublisherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PublisherController : ControllerBase
{
    private readonly ILogger<PublisherController> _logger;
    private readonly IMessagePublisher _messagePublisher;

    public PublisherController(ILogger<PublisherController> logger, IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _messagePublisher = messagePublisher;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> PublishChatMessage([FromBody] ChatMessage message)
    {
        try
        {
            _logger.LogInformation("Publishing chat message");
            await _messagePublisher.PublishAsync(message);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing chat message");
            return StatusCode(500, "Failed to publish message");
        }
    }
}
