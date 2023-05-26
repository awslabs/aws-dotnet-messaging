// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;
using AWS.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWS.Messaging.Services;

/// <inheritdoc cref="IHandlerInvoker"/>
public class HandlerInvoker : IHandlerInvoker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HandlerInvoker> _logger;

    /// <summary>
    /// Caches the <see cref="MethodInfo"/> of the <see cref="IMessageHandler{T}.HandleAsync(MessageEnvelope{T}, CancellationToken)"/>
    /// method that will be invoked with the message envelope for each handler
    /// </summary>
    private readonly ConcurrentDictionary<Type, MethodInfo?> _handlerMethods = new();

    /// <summary>
    /// Constructs an instance of HandlerInvoker
    /// </summary>
    /// <param name="serviceProvider">Service provider used to resolve handler objects</param>
    /// <param name="logger">Logger for debugging information</param>
    public HandlerInvoker(IServiceProvider serviceProvider, ILogger<HandlerInvoker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MessageProcessStatus> InvokeAsync(MessageEnvelope messageEnvelope, SubscriberMapping subscriberMapping, CancellationToken token = default)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var handler = scope.ServiceProvider.GetService(subscriberMapping.HandlerType);

            if (handler == null)
            {
                _logger.LogError("Unable to resolve a handler for {HandlerType} while handling message ID {MessageEnvelopeId}.", subscriberMapping.HandlerType, messageEnvelope.Id);
                throw new InvalidMessageHandlerSignatureException($"Unable to resolve a handler for {subscriberMapping.HandlerType} " +
                                                                  $"while handling message ID {messageEnvelope.Id}.");
            }

            var method = _handlerMethods.GetOrAdd(subscriberMapping.MessageType, x =>
            {
                return subscriberMapping.HandlerType.GetMethod(    // Look up the method on the handler type with:
                    nameof(IMessageHandler<MessageProcessStatus>.HandleAsync),              // name "HandleAsync"
                    new Type[] { messageEnvelope.GetType(), typeof(CancellationToken) });   // parameters (MessageEnvelope<MessageType>, CancellationToken)
            });

            if (method == null)
            {
                _logger.LogError("Unable to resolve a compatible HandleAsync method for {HandlerType} while handling message ID {MessageEnvelopeId}.", subscriberMapping.HandlerType, messageEnvelope.Id);
                throw new InvalidMessageHandlerSignatureException($"Unable to resolve a compatible HandleAsync method for {subscriberMapping.HandlerType} while handling message ID {messageEnvelope.Id}.");
            }

            try
            {
                var task = method.Invoke(handler, new object[] { messageEnvelope, token }) as Task<MessageProcessStatus>;

                if (task == null)
                {
                    _logger.LogError("Unexpected return type for the HandleAsync method on {HandlerType} while handling message ID {MessageEnvelopeId}. Expected {ExpectedType}", subscriberMapping.HandlerType, messageEnvelope.Id, nameof(Task<MessageProcessStatus>));
                    throw new InvalidMessageHandlerSignatureException($"Unexpected return type for the HandleAsync method on {subscriberMapping.HandlerType} while handling message ID {messageEnvelope.Id}. Expected {nameof(Task<MessageProcessStatus>)}");
                }

                return await task;
            }
            // Since we are invoking HandleAsync via reflection, we need to unwrap the TargetInvocationException
            // containing application exceptions that happened inside the IMessageHandler
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "A handler exception occurred while handling message ID {MessageId}.", messageEnvelope.Id);
                    return MessageProcessStatus.Failed();
                }
                else
                {
                    _logger.LogError(ex, "An unexpected exception occurred while handling message ID {MessageId}.", messageEnvelope.Id);
                    return MessageProcessStatus.Failed();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected exception occurred while handling message ID {MessageId}.", messageEnvelope.Id);
                return MessageProcessStatus.Failed();
            }
        }
    }
}
