// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using AWS.Messaging.Serialization;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Services;

/// <summary>
/// This is the internal implementation of <see cref="ISQSHandler"/>
/// </summary>
internal class SQSHandler : ISQSHandler
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IEnvelopeSerializer _envelopeSerializer;
    private readonly ILogger<SQSHandler> _logger;

    /// <summary>
    /// Creates an instance of <see cref="SQSHandler"/>
    /// </summary>
    public SQSHandler(IAWSClientProvider clientProvider, IEnvelopeSerializer envelopeSerializer, ILogger<SQSHandler> logger)
    {
        _sqsClient = clientProvider.GetServiceClient<IAmazonSQS>();
        _envelopeSerializer = envelopeSerializer;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<ConvertToEnvelopeResult>> ReceiveMessageAsync(ReceiveMessageRequest receiveMessageRequest, CancellationToken token = default)
    {
        var results = new List<ConvertToEnvelopeResult>();
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
                results.Add(messageEnvelopeResult);
            }
        }
        catch (AWSMessagingException)
        {
            // Swallow exceptions thrown by the framework, and rely on the thrower to log
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "An {ExceptionName} occurred while trying to receive message for SQS queue '{sqsQueueURL}'", nameof(AmazonSQSException), receiveMessageRequest.QueueUrl);

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
            _logger.LogError(ex, "An unknown exception occurred while polling {SubscriberEndpoint}", receiveMessageRequest.QueueUrl);
        }

        return results;
    }

    /// <inheritdoc/>
    /// <exception cref="MissingSQSReceiptHandleException"></exception>
    public async Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, string sqsQueueURL, CancellationToken token = default)
    {
        if (!messages.Any())
        {
            return;
        }

        var request = new DeleteMessageBatchRequest
        {
            QueueUrl = sqsQueueURL
        };

        foreach (var message in messages)
        {
            if (string.IsNullOrEmpty(message.SQSMetadata?.ReceiptHandle))
            {
                _logger.LogError("Attempted to delete message {MessageId} from {SubscriberEndpoint} without an SQS receipt handle.", message.Id, sqsQueueURL);
                throw new MissingSQSReceiptHandleException($"Attempted to delete message {message.Id} from {sqsQueueURL} without an SQS receipt handle.");
            }

            _logger.LogTrace("Preparing to delete message {MessageId} with SQS receipt handle {ReceiptHandle} from queue {SubscriberEndpoint}",
                    message.Id, message.SQSMetadata.ReceiptHandle, sqsQueueURL);
            request.Entries.Add(new DeleteMessageBatchRequestEntry()
            {
                Id = message.Id,
                ReceiptHandle = message.SQSMetadata.ReceiptHandle
            });
        }

        try
        {
            var response = await _sqsClient.DeleteMessageBatchAsync(request, token);

            foreach (var successMessage in response.Successful)
            {
                _logger.LogTrace("Deleted message {MessageId} from queue {SubscriberEndpoint} successfully", successMessage.Id, sqsQueueURL);
            }

            foreach (var failedMessage in response.Failed)
            {
                _logger.LogError("Failed to delete message {FailedMessageId} from queue {SubscriberEndpoint}: {FailedMessage}",
                    failedMessage.Id, sqsQueueURL, failedMessage.Message);
            }
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "Failed to delete message(s) [{MessageIds}] from queue {SubscriberEndpoint}",
                string.Join(", ", messages.Select(x => x.Id)), sqsQueueURL);

            // Rethrow the exception to fail fast for invalid configuration, permissioning, etc.
            if (IsSQSExceptionFatal(ex))
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected exception occurred while deleting messages from queue {SubscriberEndpoint}", sqsQueueURL);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="MissingSQSReceiptHandleException"></exception>
    public async Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, string queueUrl, int visibilityTimeout, CancellationToken token = default)
    {
        if (!messages.Any())
        {
            return;
        }

        var request = new ChangeMessageVisibilityBatchRequest
        {
            QueueUrl = queueUrl
        };

        foreach (var message in messages)
        {
            if (string.IsNullOrEmpty(message.SQSMetadata?.ReceiptHandle))
            {
                _logger.LogError("Attempted to change the visibility of message {MessageId} from {SubscriberEndpoint} without an SQS receipt handle.", message.Id, queueUrl);
                throw new MissingSQSReceiptHandleException($"Attempted to change the visibility of message {message.Id} from {queueUrl} without an SQS receipt handle.");
            }
            
            _logger.LogTrace("Preparing to extend the visibility of {MessageId} with SQS receipt handle {ReceiptHandle} by {VisibilityTimeout} seconds",
                   message.Id, message.SQSMetadata.ReceiptHandle, visibilityTimeout);
            request.Entries.Add(new ChangeMessageVisibilityBatchRequestEntry
            {
                Id = message.Id,
                ReceiptHandle = message.SQSMetadata.ReceiptHandle,
                VisibilityTimeout = visibilityTimeout
            });
        }

        try
        {
            var response = await _sqsClient.ChangeMessageVisibilityBatchAsync(request, token);

            foreach (var successMessage in response.Successful)
            {
                _logger.LogTrace("Extended the visibility of message {MessageId} on queue {SubscriberEndpoint} successfully", successMessage.Id, queueUrl);
            }

            foreach (var failedMessage in response.Failed)
            {
                _logger.LogError("Failed to extend the visibility of message {FailedMessageId} on queue {SubscriberEndpoint}: {FailedMessage}",
                    failedMessage.Id, queueUrl, failedMessage.Message);
            }
        }
        catch (AmazonSQSException ex)
        {
            _logger.LogError(ex, "Failed to extend the visibility of message(s) [{MessageIds}] on queue {SubscriberEndpoint}",
               string.Join(", ", messages.Select(x => x.Id)), queueUrl);

            // Rethrow the exception to fail fast for invalid configuration, permissioning, etc.
            if (IsSQSExceptionFatal(ex))
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected exception occurred while extending message visibility on queue {SubscriberEndpoint}", queueUrl);
        }
    }

    /// <summary>
    /// <see cref="AmazonSQSException"/> error codes that should be treated as fatal.
    /// </summary>
    private static readonly HashSet<string> _fatalSQSErrorCodes = new HashSet<string>
    {
        "InvalidAddress",   // Returned for an invalid queue URL
        "AccessDenied"      // Returned with insufficient IAM permissions to read from the configured queue
    };

    /// <summary>
    /// Determines if a given SQS exception should be treated as fatal and rethrown to the caller
    /// </summary>
    /// <param name="sqsException">SQS Exception</param>
    private bool IsSQSExceptionFatal(AmazonSQSException sqsException)
    {
        return _fatalSQSErrorCodes.Contains(sqsException.ErrorCode);
    }
}
