// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Services
{
    internal class SQSPoller : ISQSCommunicator
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<SQSPoller> _logger;
        private readonly SQSMessagePollerConfiguration _configuration;

        public SQSPoller(IAmazonSQS sqsClient, ILogger<SQSPoller> logger, SQSMessagePollerConfiguration configuration)
        {
            _sqsClient = sqsClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token)
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
                    throw new MissingSQSReceiptHandleException($"Attempted to delete message {message.Id} from {_configuration.SubscriberEndpoint} without an SQS receipt handle.");
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

        public async Task ExtendMessageVisibilityTimeoutAsync(IEnumerable<MessageEnvelope> messages, CancellationToken token)
        {
            if (!messages.Any())
            {
                return;
            }

            var request = new ChangeMessageVisibilityBatchRequest
            {
                QueueUrl = _configuration.SubscriberEndpoint
            };

            foreach (var message in messages)
            {
                if (!string.IsNullOrEmpty(message.SQSMetadata?.ReceiptHandle))
                {
                    _logger.LogTrace("Preparing to extend the visibility of {MessageId} with SQS receipt handle {ReceiptHandle} by {VisibilityTimeout} seconds",
                        message.Id, message.SQSMetadata.ReceiptHandle, _configuration.VisibilityTimeout);
                    request.Entries.Add(new ChangeMessageVisibilityBatchRequestEntry
                    {
                        Id = message.Id,
                        ReceiptHandle = message.SQSMetadata.ReceiptHandle,
                        VisibilityTimeout = _configuration.VisibilityTimeout
                    });
                }
                else
                {
                    _logger.LogError("Attempted to change the visibility of message {MessageId} from {SubscriberEndpoint} without an SQS receipt handle.", message.Id, _configuration.SubscriberEndpoint);
                    throw new MissingSQSReceiptHandleException($"Attempted to change the visibility of message {message.Id} from {_configuration.SubscriberEndpoint} without an SQS receipt handle.");
                }
            }

            try
            {
                var response = await _sqsClient.ChangeMessageVisibilityBatchAsync(request, token);

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
            catch (AmazonSQSException ex)
            {
                _logger.LogError(ex, "Failed to extend the visibility of message(s) [{MessageIds}] on queue {SubscriberEndpoint}",
                   string.Join(", ", messages.Select(x => x.Id)), _configuration.SubscriberEndpoint);

                // Rethrow the exception to fail fast for invalid configuration, permissioning, etc.
                if (IsSQSExceptionFatal(ex))
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected exception occurred while extending message visibility on queue {SubscriberEndpoint}", _configuration.SubscriberEndpoint);
            }

        }

        public async Task<List<Message>> ReceiveMessagesAsync(int numberOfMessagesToRead, CancellationToken token)
        {
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

                return receiveMessageResponse.Messages;
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

            return new List<Message>();
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
}
