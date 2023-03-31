// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Services;

/// <inheritdoc cref="IMessageManager"/>
public class DefaultMessageManager : IMessageManager
{
    private readonly IMessagePoller _messagePoller;
    private readonly IHandlerInvoker _handlerInvoker;
    private readonly ILogger<DefaultMessageManager> _logger;

    private readonly object _activeMessageCountLock = new object();
    private int _activeMessageCount;
    private readonly ManualResetEventSlim _activeMessageCountDecrementedEvent = new ManualResetEventSlim(false);

    private readonly ConcurrentDictionary<Task, MessageEnvelope> _runningHandlerTasks = new();
    private readonly object _visibilityTimeoutExtensionTaskLock = new object();
    private Task? _visibilityTimeoutExtensionTask;

    /// <summary>
    /// Constructs an instance of <see cref="DefaultMessageManager"/>
    /// </summary>
    /// <param name="messagePoller">The poller that this manager is managing messages for</param>
    /// <param name="handlerInvoker">Used to look up and invoke the correct handler for each message</param>
    /// <param name="logger">Logger for debugging information</param>
    public DefaultMessageManager(IMessagePoller messagePoller, IHandlerInvoker handlerInvoker, ILogger<DefaultMessageManager> logger)
    {
        _messagePoller = messagePoller;
        _handlerInvoker = handlerInvoker;
        _logger = logger;
    }

    /// <inheritdoc/>
    public int ActiveMessageCount {
        get
        {
            lock (_activeMessageCountLock)
            {
                return _activeMessageCount;
            }
        }
        set
        {
            lock (_activeMessageCountLock)
            {
                _logger.LogTrace("Updating {activeMessageCount} from {before} to {after}", nameof(ActiveMessageCount), ActiveMessageCount, value);

                var isDecrementing = value < _activeMessageCount;
                _activeMessageCount = value;

                // If we just decremented the active message count, signal to the poller
                // that there may be more capacity available again.
                if (isDecrementing)
                {
                    _activeMessageCountDecrementedEvent.Set();
                }
            }
        }
    }

    /// <inheritdoc/>
    public Task WaitAsync(TimeSpan timeout)
    {
        _logger.LogTrace("Beginning wait for {name} for a maximum of {timeout} seconds", nameof(_activeMessageCountDecrementedEvent), timeout.TotalSeconds);

        // TODO: Rework to avoid this synchronous wait.
        // See https://github.com/dotnet/runtime/issues/35962 for potential workarounds
        var wasSet = _activeMessageCountDecrementedEvent.Wait(timeout);

        // Don't reset if we hit the timeout
        if (wasSet)
        {
            _logger.LogTrace("{name} was set, resetting the event", nameof(_activeMessageCountDecrementedEvent));
            _activeMessageCountDecrementedEvent.Reset();
        }
        else
        {
            _logger.LogTrace("Timed out at {timeout} seconds while waiting for {name}, returning.", timeout.TotalSeconds, nameof(_activeMessageCountDecrementedEvent));
        }

        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    public async Task ProcessMessageAsync(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping, CancellationToken token = default)
    {
        ActiveMessageCount++;

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
            _logger.LogError(ex, "An unknown exception occurred while processing message ID {subscriberEndpoint}", messageEnvelope.Id);
        }

        _runningHandlerTasks.Remove(handlerTask, out _);

        if (handlerTask.IsCompletedSuccessfully)
        {
            if (handlerTask.Result.IsSuccess)
            {
                // Delete the message from the queue if it was processed successfully
                await _messagePoller.DeleteMessagesAsync(new MessageEnvelope[] { messageEnvelope });
            }
            else // the handler still finished, but returned MessageProcessStatus.Failed
            {
                _logger.LogError("Message handling completed unsuccessfully for message ID {messageId}", messageEnvelope.Id);
            }
        }
        else if (handlerTask.IsFaulted)
        {
            _logger.LogError(handlerTask.Exception, "Message handling failed unexpectedly for message ID {messageId}", messageEnvelope.Id);
        }

        ActiveMessageCount--;
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
            await Task.Delay(_messagePoller.VisibilityTimeoutExtensionInterval * 1000, token);

            // Select the message envelopes whose corresponding handler task is not yet complete
             unfinishedMessages = _runningHandlerTasks.Values;

            // TODO: The envelopes in _runningHandlerTasks may have been received at different times, we could track + extend visibility separately
            // TODO: The underlying ChangeMessageVisibilityBatch only takes up to 10 messages, we may need to slice and make multiple calls
            // TODO: Handle the race condition where a message could have finished handling and be deleted concurrently
            await _messagePoller.ExtendMessageVisibilityTimeoutAsync(unfinishedMessages);

        } while (unfinishedMessages.Count() > 0 && !token.IsCancellationRequested);
    }
}
