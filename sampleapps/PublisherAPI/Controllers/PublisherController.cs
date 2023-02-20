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
    private readonly IMessagePublisher _messagePublisher;

    public PublisherController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpPost("chatmessage", Name = "Chat Message")]
    public async Task<IActionResult> PublishChatMessage([FromBody] ChatMessage message)
    {
        if (message == null)
        {
            return BadRequest("A chat message was not used.");
        }
        if (string.IsNullOrEmpty(message.MessageDescription))
        {
            return BadRequest("The MessageDescription cannot be null or empty.");
        }

        await _messagePublisher.PublishAsync(message);

        return Ok();
    }

    [HttpPost("order", Name = "Order")]
    public async Task<IActionResult> PublishOrder([FromBody] OrderInfo message)
    {
        if (message == null)
        {
            return BadRequest("An order info was not used.");
        }
        if (string.IsNullOrEmpty(message.UserId))
        {
            return BadRequest("The MessageDescription cannot be null or empty.");
        }

        await _messagePublisher.PublishAsync(message);

        return Ok();
    }
    
    [HttpPost("fooditem", Name = "Food Item")]
    public async Task<IActionResult> PublishFoodItem([FromBody] FoodItem message)
    {
        if (message == null)
        {
            return BadRequest("A food item was not used.");
        }
        if (string.IsNullOrEmpty(message.Name))
        {
            return BadRequest("The MessageDescription cannot be null or empty.");
        }

        await _messagePublisher.PublishAsync(message);

        return Ok();
    }
}
