using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.MessageProcessing.MessagePump
{
    /// <summary>
    /// This class handles looking up the IMessageHandler for the message type and invoking the HandleAsync method.
    /// </summary>
    public class HandlerInvoker
    {
        IServiceProvider _serviceProvider;
        ILogger<HandlerInvoker> _logger;

        /// <summary>
        /// Constructs an instance of HandlerInvoker
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="logger"></param>
        public HandlerInvoker(IServiceProvider serviceProvider, ILogger<HandlerInvoker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Invoke the registered IMessageHandler for the message type.
        /// </summary>
        /// <param name="messageEnvelope"></param>
        /// <param name="messageType"></param>
        /// <param name="handlerType"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="FatalErrorException"></exception>
        public async Task<bool> InvokeAsync(MessageEnvelope messageEnvelope, Type messageType, Type handlerType, CancellationToken token = default(CancellationToken))
        {
            var handler = _serviceProvider.GetService(handlerType);

            // TODO cache MethodInfo lookup per handerType. This will require a locking possibly with a reader writer lock since most request will only need a read lock.
            var methodInfo = typeof(IMessageHandler<>).MakeGenericType(messageType).GetMethod(nameof(IMessageHandler<object>.HandleAsync));
            if(methodInfo == null)
            {
                throw new FatalErrorException("Unexpected inteface definition missing the HandleAsync method");
            }

            try
            {
                return await InvokeAsync(methodInfo, handler, messageEnvelope, token);
            }
            // Since we are using reflection to invoke the HandleAsync method we need to unwrap the TargetInvocationException wrapping
            // application exceptions that happened inside the IMessageHandler.
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                    throw e.InnerException;
                else
                    throw;
            }
        }

        private Task<bool> InvokeAsync(MethodInfo methodInfo, object handler, MessageEnvelope messageEnvelope, CancellationToken token)
        {
            var task = methodInfo.Invoke(handler, new object[] { messageEnvelope, token }) as Task<bool>;
            if (task == null)
            {
                throw new FatalErrorException("Unexpected return type for HandleAsync method which should be Task<bool>");
            }
            return task;
        }
    }
}
