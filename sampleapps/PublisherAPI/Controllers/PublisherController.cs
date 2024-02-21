// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Messaging;
using AWS.Messaging.Publishers.SNS;
using AWS.Messaging.Publishers.SQS;
using Microsoft.AspNetCore.Mvc;
using PublisherAPI.Models;

namespace PublisherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PublisherController : ControllerBase
{
    /// <summary>
    /// Generic publisher, which you can use to publish any of the configured
    /// message types when you don't need to specify any service-specific options.
    /// </summary>
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// SQS-specific publisher to use when publishing to a 
    /// FIFO queue below so that you can set the message group ID.
    /// </summary>
    private readonly ISQSPublisher _sqsPublisher;

    /// <summary>
    /// SNS-specific publisher to use when publishing to a 
    /// FIFO topic below so that you can set the message group ID.
    /// </summary>
    private readonly ISNSPublisher _snsPublisher;

    public PublisherController(IMessagePublisher messagePublisher, ISQSPublisher sqsPubliser, ISNSPublisher snsPublisher)
    {
        _messagePublisher = messagePublisher;
        _sqsPublisher = sqsPubliser;
        _snsPublisher = snsPublisher;
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

        // Publish using the generic publisher
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

        // Publish using the generic publisher
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

        // Publish using the generic publisher
        await _messagePublisher.PublishAsync(message);

        return Ok();
    }

    [HttpPost("transactioninfo", Name = "Transaction Info")]
    public async Task<IActionResult> PublishTransaction([FromBody] TransactionInfo transactionInfo)
    {
        if (transactionInfo == null)
        {
            return BadRequest("A transaction info was not used.");
        }
        if (string.IsNullOrEmpty(transactionInfo.TransactionId))
        {
            return BadRequest("The TransactionId cannot be null or empty.");
        }

        // TransactionInfo messages are mapped to a FIFO queue, so use the
        // SQS-specific publisher which allows setting the message group ID.
        await _sqsPublisher.SendAsync(transactionInfo, new SQSOptions
        {
            MessageGroupId = "group-123"
        });

        return Ok();
    }

    [HttpPost("bidinfo", Name = "Bid Info")]
    public async Task<IActionResult> PublishBid([FromBody] BidInfo bidInfo)
    {
        if (bidInfo == null)
        {
            return BadRequest("A bid info was not used.");
        }
        if (string.IsNullOrEmpty(bidInfo.BidId))
        {
            return BadRequest("The BidId cannot be null or empty.");
        }

        // BidInfo messages are mapped to a FIFO SNS topic, so use the
        // SNS-specific publisher which allows setting the message group ID.
        await _snsPublisher.PublishAsync(bidInfo, new SNSOptions
        {
            MessageGroupId = "group-123"
        });

        return Ok();
    }
}
