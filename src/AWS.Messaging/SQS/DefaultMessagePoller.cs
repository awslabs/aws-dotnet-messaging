// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
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
internal class DefaultMessagePoller : IMessagePoller
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<DefaultMessagePoller> _logger;
    private readonly SQSMessagePollerConfiguration _configuration;
    private readonly IEnvelopeSerializer _envelopeSerializer;
    private readonly ISQSCommunicator _sqsCommunicator;
    private readonly IHandlerInvoker _handlerInvoker;

    private readonly object _activeMessageCountLock = new object();
    private int _activeMessageCount;
    private readonly ManualResetEventSlim _activeMessageCountDecrementedEvent = new ManualResetEventSlim(false);

    private readonly ConcurrentDictionary<Task, MessageEnvelope> _runningHandlerTasks = new();
    private readonly object _visibilityTimeoutExtensionTaskLock = new object();
    private Task? _visibilityTimeoutExtensionTask;

    /// <summary>
    /// Maximum valid value for <see cref="ReceiveMessageRequest.MaxNumberOfMessages"/>
    /// </summary>
    private const int SQS_MAX_NUMBER_MESSAGES_TO_READ = 10;

    /// <summary>
    /// The maximum amount of time a polling iteration should pause for while waiting
    /// for in flight messages to finish processing
    /// </summary>
    private static readonly TimeSpan CONCURRENT_CAPACITY_WAIT_TIMEOUT = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Creates instance of <see cref="AWS.Messaging.SQS.DefaultMessagePoller" />
    /// </summary>
    /// <param name="logger">Logger for debugging information.</param>
    /// <param name="awsClientProvider">Provides the AWS service client from the DI container.</param>
    /// <param name="configuration">The SQS message poller configuration.</param>
    /// <param name="envelopeSerializer">Serializer used to deserialize the SQS messages</param>
    /// <param name="handlerInvoker"></param>
    public DefaultMessagePoller(
        ILogger<DefaultMessagePoller> logger,
        IAWSClientProvider awsClientProvider,
        SQSMessagePollerConfiguration configuration,
        IEnvelopeSerializer envelopeSerializer,
        IHandlerInvoker handlerInvoker)
    {
        _logger = logger;
        _sqsClient = awsClientProvider.GetServiceClient<IAmazonSQS>();
        _configuration = configuration;
        _envelopeSerializer = envelopeSerializer;

        // TODO - just quick prototype, move to DI or factory
        _sqsCommunicator = new SQSPoller(_sqsClient, new Microsoft.Extensions.Logging.Abstractions.NullLogger<SQSPoller>(), _configuration);

        _handlerInvoker = handlerInvoker;
    }


    /// <inheritdoc/>
    public async Task StartPollingAsync(CancellationToken token = default)
    {
        await PollQueue(token);
    }

    private int ActiveMessageCount
    {
        get
        {
            lock (_activeMessageCountLock)
            {
                return _activeMessageCount;
            }
        }
    }
    /// <summary>
    /// Updates <see cref="ActiveMessageCount"/> in a thread safe manner
    /// </summary>
    /// <param name="delta">Difference to apply to the current active message count</param>
    /// <returns>Updated <see cref="ActiveMessageCount"/></returns>
    private int UpdateActiveMessageCount(int delta)
    {
        lock (_activeMessageCountLock)
        {
            var newValue = _activeMessageCount + delta;
            _logger.LogTrace("Updating {ActiveMessageCount} from {Before} to {After}", nameof(ActiveMessageCount), _activeMessageCount, newValue);

            _activeMessageCount = newValue;

            // If we just decremented the active message count, signal to the poller
            // that there may be more capacity available again.
            if (delta < 0)
            {
                _activeMessageCountDecrementedEvent.Set();
            }

            return _activeMessageCount;
        }
    }

    public Task WaitAsync(TimeSpan timeout)
    {
        _logger.LogTrace("Beginning wait for {Name} for a maximum of {Timeout} seconds", nameof(_activeMessageCountDecrementedEvent), timeout.TotalSeconds);

        // TODO: Rework to avoid this synchronous wait.
        // See https://github.com/dotnet/runtime/issues/35962 for potential workarounds
        var wasSet = _activeMessageCountDecrementedEvent.Wait(timeout);

        // Don't reset if we hit the timeout
        if (wasSet)
        {
            _logger.LogTrace("{Name} was set, resetting the event", nameof(_activeMessageCountDecrementedEvent));
            _activeMessageCountDecrementedEvent.Reset();
        }
        else
        {
            _logger.LogTrace("Timed out at {Timeout} seconds while waiting for {Name}, returning.", timeout.TotalSeconds, nameof(_activeMessageCountDecrementedEvent));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Polls SQS indefinitely until cancelled
    /// </summary>
    /// <param name="token">Cancellation token to shutdown the poller.</param>
    private async Task PollQueue(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var numberOfMessagesToRead = _configuration.MaxNumberOfConcurrentMessages - ActiveMessageCount;

            // If already processing the maximum number of messages, wait for at least one to complete and then try again
            if (numberOfMessagesToRead <= 0)
            {
                _logger.LogTrace("The maximum number of {Max} concurrent messages is already being processed. " +
                    "Waiting for one or more to complete for a maximum of {Timeout} seconds before attempting to poll again.",
                    _configuration.MaxNumberOfConcurrentMessages, CONCURRENT_CAPACITY_WAIT_TIMEOUT.TotalSeconds);

                await WaitAsync(CONCURRENT_CAPACITY_WAIT_TIMEOUT);
                continue;
            }

            // Only read SQS's maximum number of messages. If configured for
            // higher concurrency, then the next iteration could read more
            if (numberOfMessagesToRead > SQS_MAX_NUMBER_MESSAGES_TO_READ)
            {
                numberOfMessagesToRead = SQS_MAX_NUMBER_MESSAGES_TO_READ;
            }

            try
            {
                var messages = await _sqsCommunicator.ReceiveMessagesAsync(numberOfMessagesToRead, token);

                foreach (var message in messages)
                {
                    var messageEnvelopeResult = await _envelopeSerializer.ConvertToEnvelopeAsync(message);

                    // Don't await this result, we want to process multiple messages concurrently
                    _ = ProcessMessageAsync(messageEnvelopeResult.Envelope, messageEnvelopeResult.Mapping, token);
                }
            }
            catch (AWSMessagingException)
            {
                // Swallow exceptions thrown by the framework, and rely on the thrower to log
            }
            catch (Exception ex)
            {
                // TODO: explore a "cool down mode" for repeated exceptions
                _logger.LogError(ex, "An unknown exception occurred while polling {SubscriberEndpoint}", _configuration.SubscriberEndpoint);
            }
        }
    }

    public async Task ProcessMessageAsync(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping, CancellationToken token = default)
    {
        UpdateActiveMessageCount(1);

        var handlerTask = _handlerInvoker.InvokeAsync(messageEnvelope, subscriberMapping, token);

        // Add it to the dictionary of running task, used to extend the visibility timeout if necessary
        _runningHandlerTasks.TryAdd(handlerTask, messageEnvelope);

        StartMessageVisibilityExtensionTaskIfNotRunning(token);

        // Wait for the handler to finish processing the message
        try
        {
            await handlerTask;
        }
        catch (AWSMessagingException)
        {
            // Swallow exceptions thrown by the framework, and rely on the thrower to log
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unknown exception occurred while processing message ID {SubscriberEndpoint}", messageEnvelope.Id);
        }

        _runningHandlerTasks.Remove(handlerTask, out _);

        if (handlerTask.IsCompletedSuccessfully)
        {
            if (handlerTask.Result.IsSuccess)
            {
                // Delete the message from the queue if it was processed successfully
                await _sqsCommunicator.DeleteMessagesAsync(new MessageEnvelope[] { messageEnvelope });
            }
            else // the handler still finished, but returned MessageProcessStatus.Failed
            {
                _logger.LogError("Message handling completed unsuccessfully for message ID {MessageId}", messageEnvelope.Id);
            }
        }
        else if (handlerTask.IsFaulted)
        {
            _logger.LogError(handlerTask.Exception, "Message handling failed unexpectedly for message ID {MessageId}", messageEnvelope.Id);
        }

        UpdateActiveMessageCount(-1);
    }

    /// <summary>
    /// Starts the task that extends the visibility timeout of in-flight messages
    /// </summary>
    /// <param name="token">Cancellation token to stop the visibility timeout extension task</param>
    private void StartMessageVisibilityExtensionTaskIfNotRunning(CancellationToken token)
    {
        // It may either have been never started, or previously started and completed because there were no more in flight messages
        if (_visibilityTimeoutExtensionTask == null || _visibilityTimeoutExtensionTask.IsCompleted)
        {
            lock (_visibilityTimeoutExtensionTaskLock)
            {
                if (_visibilityTimeoutExtensionTask == null || _visibilityTimeoutExtensionTask.IsCompleted)
                {
                    _visibilityTimeoutExtensionTask = ExtendUnfinishedMessageVisibilityTimeoutBatch(token);
                }
            }
        }
    }

    /// <summary>
    /// Extends the visibility timeout periodically for messages whose corresponding handler task is not yet complete
    /// </summary>
    /// <param name="token">Cancellation token to stop the visibility timeout extension loop</param>
    private async Task ExtendUnfinishedMessageVisibilityTimeoutBatch(CancellationToken token)
    {
        IEnumerable<MessageEnvelope> unfinishedMessages;

        do
        {
            // Wait for the configured interval before extending visibility
            await Task.Delay(_configuration.VisibilityTimeoutExtensionInterval * 1000, token);

            // Select the message envelopes whose corresponding handler task is not yet complete
            unfinishedMessages = _runningHandlerTasks.Values;

            // TODO: The envelopes in _runningHandlerTasks may have been received at different times, we could track + extend visibility separately
            // TODO: The underlying ChangeMessageVisibilityBatch only takes up to 10 messages, we may need to slice and make multiple calls
            // TODO: Handle the race condition where a message could have finished handling and be deleted concurrently
            await _sqsCommunicator.ExtendMessageVisibilityTimeoutAsync(unfinishedMessages);

        } while (unfinishedMessages.Any() && !token.IsCancellationRequested);
    }
}
