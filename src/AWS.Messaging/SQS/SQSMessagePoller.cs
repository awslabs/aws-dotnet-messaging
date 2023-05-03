// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using AWS.Messaging.Services;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.SQS;

/// <summary>
/// SQS implementation of the <see cref="AWS.Messaging.Services.IMessagePoller" />
/// </summary>
internal class SQSMessagePoller : IMessagePoller
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SQSMessagePoller> _logger;
    private readonly IMessageManager _messageManager;
    private readonly SQSMessagePollerConfiguration _configuration;
    private readonly IEnvelopeSerializer _envelopeSerializer;

    /// <summary>
    /// Maximum valid value for <see cref="ReceiveMessageRequest.MaxNumberOfMessages"/>
    /// </summary>
    private const int SQS_MAX_NUMBER_MESSAGES_TO_READ = 10;

    /// <summary>
    /// Maximum valid value for number of messages in <see cref="ChangeMessageVisibilityBatchRequest"/>
    /// </summary>
    private const int SQS_MAX_MESSAGE_CHANGE_VISIBILITY = 10;

    /// <summary>
    /// The maximum amount of time a polling iteration should pause for while waiting
    /// for in flight messages to finish processing
    /// </summary>
    private static readonly TimeSpan CONCURRENT_CAPACITY_WAIT_TIMEOUT = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Creates instance of <see cref="AWS.Messaging.SQS.SQSMessagePoller" />
    /// </summary>
    /// <param name="logger">Logger for debugging information.</param>
    /// <param name="messageManagerFactory">The factory to create the message manager for processing messages.</param>
    /// <param name="awsClientProvider">Provides the AWS service client from the DI container.</param>
    /// <param name="configuration">The SQS message poller configuration.</param>
    /// <param name="envelopeSerializer">Serializer used to deserialize the SQS messages</param>
    public SQSMessagePoller(
        ILogger<SQSMessagePoller> logger,
        IMessageManagerFactory messageManagerFactory,
        IAWSClientProvider awsClientProvider,
        SQSMessagePollerConfiguration configuration,
        IEnvelopeSerializer envelopeSerializer)
    {
        _logger = logger;
        _sqsClient = awsClientProvider.GetServiceClient<IAmazonSQS>();
        _configuration = configuration;
        _envelopeSerializer = envelopeSerializer;

        _messageManager = messageManagerFactory.CreateMessageManager(this);
    }

    /// <inheritdoc/>
    public bool ShouldExtendVisibilityTimeout => true;

    /// <inheritdoc/>
    public int VisibilityTimeoutExtensionInterval => _configuration.VisibilityTimeoutExtensionInterval;


    /// <inheritdoc/>
    public async Task StartPollingAsync(CancellationToken token = default)
    {
        await PollQueue(token);
    }

    /// <summary>
    /// Polls SQS indefinitely until cancelled
    /// </summary>
    /// <param name="token">Cancellation token to shutdown the poller.</param>
    private async Task PollQueue(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
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

            // Only read SQS's maximum number of messages. If configured for
            // higher concurrency, then the next iteration could read more
            if (numberOfMessagesToRead > SQS_MAX_NUMBER_MESSAGES_TO_READ)
            {
                numberOfMessagesToRead = SQS_MAX_NUMBER_MESSAGES_TO_READ;
            }

            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = _configuration.SubscriberEndpoint,
                VisibilityTimeout = _configuration.VisibilityTimeout,
                WaitTimeSeconds = _configuration.WaitTimeSeconds,
                MaxNumberOfMessages = numberOfMessagesToRead,
                AttributeNames = new List<string> { "All" },
                MessageAttributeNames = new List<string> { "All" }
            };

            try
            {
                _logger.LogTrace("Retrieving up to {NumberOfMessagesToRead} messages from {QueueUrl}",
                    receiveMessageRequest.MaxNumberOfMessages, receiveMessageRequest.QueueUrl);

                var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, token);

                _logger.LogTrace("Retrieved {MessagesCount} messages from {QueueUrl} via request ID {RequestId}",
                    receiveMessageResponse.Messages.Count, receiveMessageRequest.QueueUrl, receiveMessageResponse.ResponseMetadata.RequestId);

                foreach (var message in receiveMessageResponse.Messages)
                {
                    var messageEnvelopeResult = await _envelopeSerializer.ConvertToEnvelopeAsync(message);

                    // Don't await this result, we want to process multiple messages concurrently
                    _ = _messageManager.ProcessMessageAsync(messageEnvelopeResult.Envelope, messageEnvelopeResult.Mapping, token);
                }
            }
            catch (AWSMessagingException)
            {
                // Swallow exceptions thrown by the framework, and rely on the thrower to log
            }
            catch (AmazonSQSException ex)
            {
                _logger.LogError(ex, "An {ExceptionName} occurred while polling", nameof(AmazonSQSException));

                // Rethrow the exception to fail fast for invalid configuration, permissioning, etc.
                // TODO: explore a "cool down mode" for repeated exceptions
                if (IsSQSExceptionFatal(ex))
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                // TODO: explore a "cool down mode" for repeated exceptions
                _logger.LogError(ex, "An unknown exception occurred while polling {SubscriberEndpoint}", _configuration.SubscriberEndpoint);
            }
        }
    }

    /// <inheritdoc/>
    public async Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default)
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
            if (!string.IsNullOrEmpty(message.SQSMetadata?.ReceiptHandle))
            {
                _logger.LogTrace("Preparing to delete message {MessageId} with SQS receipt handle {ReceiptHandle} from queue {SubscriberEndpoint}",
                    message.Id, message.SQSMetadata.ReceiptHandle, _configuration.SubscriberEndpoint);
                request.Entries.Add(new DeleteMessageBatchRequestEntry()
                {
                    Id = message.Id,
                    ReceiptHandle = message.SQSMetadata.ReceiptHandle
                });
            }
            else
            {
                _logger.LogError("Attempted to delete message {MessageId} from {SubscriberEndpoint} without an SQS receipt handle.", message.Id, _configuration.SubscriberEndpoint);
                throw new MissingSQSMetadataException($"Attempted to delete message {message.Id} from {_configuration.SubscriberEndpoint} without an SQS receipt handle.");
            }
        }

        try
        {
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
    public async Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token = default)
    {
        if (!messages.Any())
        {
            return;
        }

        var requestBatches = new List<ChangeMessageVisibilityBatchRequest>();

        var currentRequest = new ChangeMessageVisibilityBatchRequest
        {
            QueueUrl = _configuration.SubscriberEndpoint
        };
        foreach (var message in messages)
        {
            if (!string.IsNullOrEmpty(message.SQSMetadata?.ReceiptHandle))
            {
                _logger.LogTrace("Preparing to extend the visibility of {MessageId} with SQS receipt handle {ReceiptHandle} by {VisibilityTimeout} seconds",
                    message.Id, message.SQSMetadata.ReceiptHandle, _configuration.VisibilityTimeout);
                if (currentRequest.Entries.Count >= SQS_MAX_MESSAGE_CHANGE_VISIBILITY)
                {
                    requestBatches.Add(currentRequest);
                    currentRequest = new ChangeMessageVisibilityBatchRequest
                    {
                        QueueUrl = _configuration.SubscriberEndpoint
                    };
                }
                currentRequest.Entries.Add(new ChangeMessageVisibilityBatchRequestEntry
                {
                    Id = message.Id,
                    ReceiptHandle = message.SQSMetadata.ReceiptHandle,
                    VisibilityTimeout = _configuration.VisibilityTimeout
                });
            }
            else
            {
                _logger.LogError("Attempted to change the visibility of message {MessageId} from {SubscriberEndpoint} without an SQS receipt handle.", message.Id, _configuration.SubscriberEndpoint);
                throw new MissingSQSMetadataException($"Attempted to change the visibility of message {message.Id} from {_configuration.SubscriberEndpoint} without an SQS receipt handle.");
            }
        }
        requestBatches.Add(currentRequest);

        List<Task<ChangeMessageVisibilityBatchResponse>> changeMessageVisibilityBatchTasks =
            requestBatches
            .Select(request => _sqsClient.ChangeMessageVisibilityBatchAsync(request, token))
            .ToList();

        try
        {
            var responses = await Task.WhenAll(changeMessageVisibilityBatchTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected exception occurred while extending message visibility on queue {SubscriberEndpoint}", _configuration.SubscriberEndpoint);
        }

        foreach (var changeMessageVisibilityBatchTask in changeMessageVisibilityBatchTasks)
        {
            if (!changeMessageVisibilityBatchTask.IsFaulted)
            {
                var response = changeMessageVisibilityBatchTask.Result;
                foreach (var successMessage in response.Successful)
                {
                    _logger.LogTrace("Extended the visibility of message {MessageId} on queue {SubscriberEndpoint} successfully", successMessage.Id, _configuration.SubscriberEndpoint);
                }

                foreach (var failedMessage in response.Failed)
                {
                    _logger.LogError("Failed to extend the visibility of message {FailedMessageId} on queue {SubscriberEndpoint}: {FailedMessage}",
                        failedMessage.Id, _configuration.SubscriberEndpoint, failedMessage.Message);
                }
            }
            else
            {
                if (changeMessageVisibilityBatchTask.Exception?.InnerException is AmazonSQSException amazonEx)
                {
                    _logger.LogError(amazonEx, "Failed to extend the visibility of message(s) [{MessageIds}] on queue {SubscriberEndpoint}",
                        string.Join(", ", messages.Select(x => x.Id)), _configuration.SubscriberEndpoint);

                    // Rethrow the exception to fail fast for invalid configuration, permissioning, etc.
                    if (IsSQSExceptionFatal(amazonEx))
                    {
                        throw amazonEx;
                    }
                }
                else if (changeMessageVisibilityBatchTask.Exception?.InnerException is Exception ex)
                {
                    _logger.LogError(ex, "An unexpected exception occurred while extending message visibility on queue {SubscriberEndpoint}", _configuration.SubscriberEndpoint);
                }
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>This is a no-op since we currently do not have any special logic to handle messages that failed to process in <see cref="SQSMessagePoller"/></remarks>
    public ValueTask HandleMessageProcessingFailureAsync(MessageEnvelope message)
    {
        return ValueTask.CompletedTask;
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
