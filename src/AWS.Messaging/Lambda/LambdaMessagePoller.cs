// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Lambda;

internal class LambdaMessagePoller : IMessagePoller
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<LambdaMessagePoller> _logger;
    private readonly IMessageManager _messageManager;
    private readonly LambdaMessagePollerConfiguration _configuration;
    private readonly IEnvelopeSerializer _envelopeSerializer;

    // this is used to safely delete messages from _configuration.SQSBatchResponse.
    private readonly object _sqsBatchResponseLock = new object();

    /// <summary>
    /// Creates instance of <see cref="LambdaMessagePoller" />
    /// </summary>
    /// <param name="logger">Logger for debugging information.</param>
    /// <param name="messageManagerFactory">The factory to create the message manager for processing messages.</param>
    /// <param name="awsClientProvider">Provides the AWS service client from the DI container.</param>
    /// <param name="configuration">The Lambda message poller configuration.</param>
    /// <param name="envelopeSerializer">Serializer used to deserialize the SQS messages</param>
    public LambdaMessagePoller(
        ILogger<LambdaMessagePoller> logger,
        IMessageManagerFactory messageManagerFactory,
        IAWSClientProvider awsClientProvider,
        LambdaMessagePollerConfiguration configuration,
        IEnvelopeSerializer envelopeSerializer)
    {
        _logger = logger;
        _sqsClient = awsClientProvider.GetServiceClient<IAmazonSQS>();
        _configuration = configuration;
        _envelopeSerializer = envelopeSerializer;

        _messageManager = messageManagerFactory.CreateMessageManager(this);
    }

    /// <inheritdoc/>
    public bool ShouldExtendVisibilityTimeout => false;

    /// <inheritdoc/>
    /// <remarks>
    /// This parameter does not hold any significance since <see cref="ShouldExtendVisibilityTimeout"/> is set to false.
    /// </remarks>
    public int VisibilityTimeoutExtensionInterval => 0;

    /// <summary>
    /// The maximum amount of time a polling iteration should pause for while waiting
    /// for in flight messages to finish processing
    /// </summary>
    private static readonly TimeSpan CONCURRENT_CAPACITY_WAIT_TIMEOUT = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    public async Task StartPollingAsync(CancellationToken token = default)
    {
        var sqsEvent = _configuration.SQSEvent;
        if (sqsEvent is null || !sqsEvent.Records.Any())
        {
            return;
        }

        var taskList = new List<Task>();
        var index = 0;

        while (!token.IsCancellationRequested && index < sqsEvent.Records.Count)
        {
            var numberOfMessagesToRead = _configuration.MaxNumberOfConcurrentMessages - _messageManager.ActiveMessageCount;

            // If already processing the maximum number of messages, wait for at least one to complete and then try again
            if (numberOfMessagesToRead <= 0)
            {
                _logger.LogTrace("The maximum number of {Max} concurrent messages is already being processed. " +
                    "Waiting for one or more to complete for a maximum of {Timeout} seconds before attempting to poll again.",
                    _configuration.MaxNumberOfConcurrentMessages, CONCURRENT_CAPACITY_WAIT_TIMEOUT.TotalSeconds);

                await _messageManager.WaitAsync(CONCURRENT_CAPACITY_WAIT_TIMEOUT);
                continue;
            }

            var message = ConvertToStandardSQSMessage(sqsEvent.Records[index]);
            var messageEnvelopeResult = await _envelopeSerializer.ConvertToEnvelopeAsync(message);

            // Don't await this result, we want to process multiple messages concurrently
            var task = _messageManager.ProcessMessageAsync(messageEnvelopeResult.Envelope, messageEnvelopeResult.Mapping, token);
            taskList.Add(task);
            index++;
        }

        await Task.WhenAll(taskList);
    }

    public async Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default)
    {
        try
        {
            // SQSBatchResponse will be pre-filled when a user invokes the Lambda message pump with ExecuteWithSQSBatchResponseAsync.
            if (_configuration.SQSBatchResponse is not null)
            {
                DeleteMessagesFromSQSBatchResponse(messages);
                return;
            }
            await DeleteMessagesFromSQSQueue(messages, token);
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "Failed to delete message(s) [{MessageIds}] from queue {SubscriberEndpoint}",
                string.Join(", ", messages.Select(x => x.Id)), _configuration.SubscriberEndpoint);

            // Rethrow the exception to fail fast for invalid configuration, permissioning, etc.
            if (IsSQSExceptionFatal(ex))
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected exception occurred while deleting messages from queue {SubscriberEndpoint}", _configuration.SubscriberEndpoint);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This is a no-op since <see cref="ShouldExtendVisibilityTimeout"/> is set to false.
    /// </remarks>
    public Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }

    private void DeleteMessagesFromSQSBatchResponse(IEnumerable<MessageEnvelope> messages)
    {
        lock (_sqsBatchResponseLock)
        {
            var batchItemFailures = _configuration.SQSBatchResponse!.BatchItemFailures;
            foreach (var message in messages)
            {
                var messageID = message.SQSMetadata?.MessageID ?? throw new InvalidDataException($"message envelope with ID '{message.Id}' contains an null SQS message ID");
                var index = batchItemFailures.FindIndex(x => string.Equals(x.ItemIdentifier, messageID));
                if (index == -1)
                {
                    _logger.LogError("Could not find an entry with message ID '{messageID}' in the batchItemFailures list", messageID);
                    continue;
                }

                _logger.LogTrace("Removing message with ID '{messageID}' from the batchItemFailures list since it was successfully processed", messageID);
                batchItemFailures.RemoveAt(index);
            }
        }
    }

    private async Task DeleteMessagesFromSQSQueue(IEnumerable<MessageEnvelope> messages, CancellationToken token = default)
    {
        if (!messages.Any())
        {
            return;
        }

        var request = new DeleteMessageBatchRequest
        {
            QueueUrl = _configuration.SubscriberEndpoint
        };

        foreach (var message in messages)
        {
            if (string.IsNullOrEmpty(message.SQSMetadata?.ReceiptHandle))
            {
                _logger.LogError("Attempted to delete message {MessageId} from {SubscriberEndpoint} without an SQS receipt handle.", message.Id, _configuration.SubscriberEndpoint);
                throw new MissingSQSReceiptHandleException($"Attempted to delete message {message.Id} from {_configuration.SubscriberEndpoint} without an SQS receipt handle.");
            }

            _logger.LogTrace("Preparing to delete message {MessageId} with SQS receipt handle {ReceiptHandle} from queue {SubscriberEndpoint}",
                   message.Id, message.SQSMetadata.ReceiptHandle, _configuration.SubscriberEndpoint);
            request.Entries.Add(new DeleteMessageBatchRequestEntry()
            {
                Id = message.Id,
                ReceiptHandle = message.SQSMetadata.ReceiptHandle
            });
        }

        var response = await _sqsClient.DeleteMessageBatchAsync(request, token);

        foreach (var successMessage in response.Successful)
        {
            _logger.LogTrace("Deleted message {MessageId} from queue {SubscriberEndpoint} successfully", successMessage.Id, _configuration.SubscriberEndpoint);
        }

        foreach (var failedMessage in response.Failed)
        {
            _logger.LogError("Failed to delete message {FailedMessageId} from queue {SubscriberEndpoint}: {FailedMessage}",
                failedMessage.Id, _configuration.SubscriberEndpoint, failedMessage.Message);
        }
    }

    private Message ConvertToStandardSQSMessage(SQSEvent.SQSMessage sqsEventMessage)
    {
        var sqsMessage = new Message
        {
            Attributes = sqsEventMessage.Attributes,
            Body = sqsEventMessage.Body,
            MD5OfBody = sqsEventMessage.Md5OfBody,
            MD5OfMessageAttributes = sqsEventMessage.Md5OfMessageAttributes,
            MessageId = sqsEventMessage.MessageId,
            ReceiptHandle = sqsEventMessage.ReceiptHandle,
        };

        foreach (var attr in sqsEventMessage.MessageAttributes)
        {
            sqsMessage.MessageAttributes.Add(attr.Key, new MessageAttributeValue
            {
                BinaryListValues = attr.Value.BinaryListValues,
                BinaryValue = attr.Value.BinaryValue,
                DataType = attr.Value.DataType,
                StringListValues = attr.Value.StringListValues,
                StringValue = attr.Value.StringValue,
            });
        }

        return sqsMessage;
    }

    /// <summary>
    /// <see cref="AmazonSQSException"/> error codes that should be treated as fatal and stop the poller
    /// </summary>
    private static readonly HashSet<string> _fatalSQSErrorCodes = new HashSet<string>
    {
        "InvalidAddress",   // Returned for an invalid queue URL
        "AccessDenied"      // Returned with insufficient IAM permissions to read from the configured queue
    };

    /// <summary>
    /// Determines if a given SQS exception should be treated as fatal and rethrown to stop the poller
    /// </summary>
    /// <param name="sqsException">SQS Exception</param>
    private bool IsSQSExceptionFatal(AmazonSQSException sqsException)
    {
        return _fatalSQSErrorCodes.Contains(sqsException.ErrorCode);
    }
}

