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
    private readonly MessageManagerConfiguration _configuration;

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
    /// <param name="configuration">The configuration for the message manager</param>
    public DefaultMessageManager(IMessagePoller messagePoller, IHandlerInvoker handlerInvoker, ILogger<DefaultMessageManager> logger, MessageManagerConfiguration configuration)
    {
        _messagePoller = messagePoller;
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
                await _messagePoller.DeleteMessagesAsync(new MessageEnvelope[] { messageEnvelope });
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
        IEnumerable<MessageEnvelope> unfinishedMessages;

        do
        {
            // Wait for the configured interval before extending visibility
            await Task.Delay(_configuration.VisibilityTimeoutExtensionHeartbeatInterval, token);

            // Select the message envelopes whose corresponding handler task is not yet complete
            unfinishedMessages = _runningHandlerTasks.Values.Where(message => IsMessageVisibilityTimeoutExpiring(message));

            // TODO: Handle the race condition where a message could have finished handling and be deleted concurrently
            if (unfinishedMessages.Any())
            {
                await _messagePoller.ExtendMessageVisibilityTimeoutAsync(unfinishedMessages, token);
            }

        } while (_runningHandlerTasks.Any() && !token.IsCancellationRequested);
    }

    /// <summary>
    /// Determines if a given message's visibility timeout expiration timestamp is within the
    /// threshold where the framework should extend it
    /// </summary>
    /// <param name="message">In flight message envelope</param>
    /// <returns>True if the message's visibility timeout should be extended per the configured threshold, false otherwise</returns>
    private bool IsMessageVisibilityTimeoutExpiring(MessageEnvelope message)
    {
        if (message.SQSMetadata == null || message.SQSMetadata.ExpectedVisibilityTimeoutExpiration == null)
        {
            _logger.LogError("Attempted to manage the expected visibility timeout of message {MessageId} without SQS metadata.", message.Id);
            throw new MissingSQSMetadataException($"Attempted to manage the expected visibility timeout of message {message.Id} without SQS metadata.");
        }

        var timeUntilExpiration = message.SQSMetadata.ExpectedVisibilityTimeoutExpiration - DateTimeOffset.UtcNow;

        if (timeUntilExpiration.Value.TotalSeconds <= _configuration.VisibilityTimeoutExtensionThreshold)
        {
            return true;
        }

        return false;
    }
}
