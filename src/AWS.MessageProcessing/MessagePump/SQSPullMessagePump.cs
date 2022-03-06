using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.MessageProcessing.Configuration;
using AWS.MessageProcessing.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWS.MessageProcessing.MessagePump
{
    /// <summary>
    /// SQSPullMessagePump is used to pull messages from an SQS queue and dispatch the messages to the appropiate IMessageHandler.
    /// This class is intended to run for the length of the process polling an individual queue.
    /// </summary>
    public class SQSPullMessagePump
    {
        IServiceProvider _serviceProvider;
        IAmazonSQS _sqsClient;
        ILogger<SQSPullMessagePump> _logger;
        SerializationUtilties _serializationUtilties;
        SQSPollerConfiguration _sqsPollerConfiguration;
        HandlerInvoker _handlerInvoker;

        /// <summary>
        /// Constructs an instance of SQSPullMessagePump
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="logger"></param>
        /// <param name="sqsClient"></param>
        /// <param name="sqsPollerConfiguration"></param>
        public SQSPullMessagePump(IServiceProvider serviceProvider, ILogger<SQSPullMessagePump> logger, IAmazonSQS sqsClient, SQSPollerConfiguration sqsPollerConfiguration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _serializationUtilties = (SerializationUtilties)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(SerializationUtilties));
            _sqsClient = sqsClient;
            _sqsPollerConfiguration = sqsPollerConfiguration;

            _handlerInvoker = (HandlerInvoker)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(HandlerInvoker));
        }

        /// <summary>
        /// Starts the poller on the SQS queue.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken token)
        {
            await PollQueue(_sqsPollerConfiguration.QueueUrl, token);
        }

        /// <summary>
        /// TODO: This is currently very naive polling with no parallaziation support and minimal error handling. We should also think about batching up
        /// delete message calls. If the message is taking too long then we need to extend the message visiblity.
        /// </summary>
        /// <param name="queueUrl"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task PollQueue(string queueUrl, CancellationToken token)
        {
            _logger.LogTrace($"Starting SQS polling for queue url {queueUrl}");
            var receiveRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                VisibilityTimeout = 20,
                WaitTimeSeconds = 20
            };

            ulong pollCount = 0;
            ulong successCount = 0;
            ulong errorCount = 0;
            while(!token.IsCancellationRequested)
            {
                if(pollCount == ulong.MaxValue)
                {
                    pollCount = 0;
                }
                if (successCount == ulong.MaxValue)
                {
                    successCount = 0;
                }
                if (errorCount == ulong.MaxValue)
                {
                    errorCount = 0;
                }

                pollCount++;
                try
                {
                    _logger.LogTrace($"Making polling call (poll calls: {pollCount}, successes: {successCount}, errors: {errorCount})");

                    // TODO: Handle errors and retries. If it permission issue or queue doesn't exists then fail fast. If there was a network issues pause the pump for sometime and then restart.
                    // TODO: Add token to user agent string.
                    var receiveResponses = (await _sqsClient.ReceiveMessageAsync(receiveRequest, token));
                    _logger.LogTrace($"Receive call returned: (AWS Request ID) {receiveResponses.ResponseMetadata.RequestId}, (Message Count) {receiveResponses.Messages.Count}");

                    foreach(var message in receiveResponses.Messages)
                    {
                        SerializationUtilties.ConvertResults convertResults;
                        try
                        {
                            convertResults = _serializationUtilties.ConvertToEnvelopeMessage(message);                           
                        }
                        catch(InvalidMessageFormatException e)
                        {
                            errorCount++;
                            _logger.LogError(e, e.Message);
                            continue;
                        }

                        try
                        {
                            var invokeSuccess = await _handlerInvoker.InvokeAsync(convertResults.MessageEnvelope, convertResults.Mapping.MessageType, convertResults.Mapping.HandlerType, token);
                            if(!invokeSuccess)
                            {
                                _logger.LogInformation($"Handler for message (id: {convertResults.MessageEnvelope.Id}) reported the message was not successfully processed");
                                errorCount++;
                                continue;
                            }
                        }
                        catch(FatalErrorException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            _logger.LogError(e, $"Exception processing message (id: {convertResults.MessageEnvelope.Id})\n{message.Body}");
                            continue;
                        }

                        try
                        {
                            await _sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, token);
                        }
                        catch(AmazonSQSException e)
                        {
                            errorCount++;
                            _logger.LogError(e, $"Failed to delete message from SQS queue: {message.ReceiptHandle}");
                            continue;
                        }

                        successCount++;
                    }
                }
                catch (TaskCanceledException)
                {
                    if(token.IsCancellationRequested)
                    {
                        _logger.LogTrace($"Shutting down SQS pull message pump. (poll calls: {pollCount}, successes: {successCount}, errors: {errorCount})");
                        return;
                    }

                    // TODO handle retry
                    throw;
                }
                catch(Exception)
                {
                    // TODO: Handle unknown errors.
                    throw;
                }
            }
        }
    }
}
