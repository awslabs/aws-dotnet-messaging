// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Collections.Generic;
using AWS.Messaging.Configuration;
using AWS.Messaging.SQS;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Services;

/// <inheritdoc cref="IMessageManager"/>
public class DefaultMessageManager : IMessageManager
{
    private readonly ISQSMessageCommunication _sqsMessageCommunication;
    private readonly IHandlerInvoker _handlerInvoker;
    private readonly ILogger<DefaultMessageManager> _logger;
    private readonly MessageManagerConfiguration _configuration;

    private readonly object _activeMessageCountLock = new object();
    private int _activeMessageCount;
    private readonly ManualResetEventSlim _activeMessageCountDecrementedEvent = new ManualResetEventSlim(false);

    private readonly ConcurrentDictionary<MessageEnvelope, InFlightMetadata> _inFlightMessageMetadata = new();
    private readonly object _visibilityTimeoutExtensionTaskLock = new object();
    private Task? _visibilityTimeoutExtensionTask;

    /// <summary>
    /// Constructs an instance of <see cref="DefaultMessageManager"/>
    /// </summary>
    /// <param name="sqsMessageCommunication">Provides APIs to communicate back to SQS and the associated Queue for incoming messages.</param>
    /// <param name="handlerInvoker">Used to look up and invoke the correct handler for each message</param>
    /// <param name="logger">Logger for debugging information</param>
    /// <param name="configuration">The configuration for the message manager</param>
    public DefaultMessageManager(ISQSMessageCommunication sqsMessageCommunication, IHandlerInvoker handlerInvoker, ILogger<DefaultMessageManager> logger, MessageManagerConfiguration configuration)
    {
        _sqsMessageCommunication = sqsMessageCommunication;
        _handlerInvoker = handlerInvoker;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public int ActiveMessageCount
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task ProcessMessageAsync(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping, CancellationToken token = default)
    {
        UpdateActiveMessageCount(1);

        // Start the handler task (but not await it), and set the timestamp that the initial visibility timeout window is expected to expire
        var metadata = new InFlightMetadata(
            _handlerInvoker.InvokeAsync(messageEnvelope, subscriberMapping, token),
            DateTimeOffset.UtcNow + TimeSpan.FromSeconds(_configuration.VisibilityTimeout)
        );

        // Add that metadata to the dictionary of running handlers, used to extend the visibility timeout if necessary
        _inFlightMessageMetadata.TryAdd(messageEnvelope, metadata);

        if (_configuration.SupportExtendingVisibilityTimeout)
            StartMessageVisibilityExtensionTaskIfNotRunning(token);

        // Wait for the handler to finish processing the message
        try
        {
            await metadata.HandlerTask;
        }
        catch (AWSMessagingException)
        {
            // Swallow exceptions thrown by the framework, and rely on the thrower to log
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unknown exception occurred while processing message ID {SubscriberEndpoint}", messageEnvelope.Id);
        }

        _inFlightMessageMetadata.Remove(messageEnvelope, out _);

        if (metadata.HandlerTask.IsCompletedSuccessfully)
        {
            if (metadata.HandlerTask.Result.IsSuccess)
            {
                // Delete the message from the queue if it was processed successfully
                await _sqsMessageCommunication.DeleteMessagesAsync(new MessageEnvelope[] { messageEnvelope });
            }
            else // the handler still finished, but returned MessageProcessStatus.Failed
            {
                _logger.LogError("Message handling completed unsuccessfully for message ID {MessageId}", messageEnvelope.Id);
                await _sqsMessageCommunication.ReportMessageFailureAsync(messageEnvelope);
            }
        }
        else if (metadata.HandlerTask.IsFaulted)
        {
            _logger.LogError(metadata.HandlerTask.Exception, "Message handling failed unexpectedly for message ID {MessageId}", messageEnvelope.Id);
            await _sqsMessageCommunication.ReportMessageFailureAsync(messageEnvelope);
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
            lock(_visibilityTimeoutExtensionTaskLock)
            {
                if (_visibilityTimeoutExtensionTask == null || _visibilityTimeoutExtensionTask.IsCompleted)
                {
                    _visibilityTimeoutExtensionTask = ExtendUnfinishedMessageVisibilityTimeoutBatch(token);

                    _logger.LogTrace("Started task with id {id} to extend the visibility timeout of in flight messages", _visibilityTimeoutExtensionTask.Id);
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
        IEnumerable<KeyValuePair<MessageEnvelope, InFlightMetadata>> unfinishedMessages;

        do
        {
            // Wait for the configured interval before extending visibility
            await Task.Delay(_configuration.VisibilityTimeoutExtensionHeartbeatInterval, token);

            // Select the message envelopes whose corresponding handler task is not yet complete.
            //
            //   The .ToList() is important as we want to evaluate their expected expiration relative to the threshold at this instant,
            //   rather than lazily. Otherwise the batch of messages that are eligible for a visibility timeout extension may change between
            //   actually extending the timeout and recording that we've done so.
            unfinishedMessages = _inFlightMessageMetadata.Where(messageAndMetadata =>
                messageAndMetadata.Value.IsMessageVisibilityTimeoutExpiring(_configuration.VisibilityTimeoutExtensionThreshold)).ToList();

            // TODO: Handle the race condition where a message could have finished handling and be deleted concurrently
            if (unfinishedMessages.Any())
            {
                // Update the timestamp that the visibility timeout window is expected to expire
                // Per SQS documentation: "The new timeout period takes effect from the time you call the ChangeMessageVisibility action"
                foreach (var unfinishedMessage in unfinishedMessages)
                {
                    unfinishedMessage.Value.UpdateExpectedVisibilityTimeoutExpiration(_configuration.VisibilityTimeout);
                }

                await _sqsMessageCommunication.ExtendMessageVisibilityTimeoutAsync(unfinishedMessages.Select(messageAndMetadata => messageAndMetadata.Key), token);
            }
        } while (_inFlightMessageMetadata.Any() && !token.IsCancellationRequested);
    }
}
