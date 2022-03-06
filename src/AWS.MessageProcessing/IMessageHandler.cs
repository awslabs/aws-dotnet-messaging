using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.MessageProcessing
{
    /// <summary>
    /// A handler that application developers implement that will process messages of type <T>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageHandler<T>
    {
        /// <summary>
        /// Method called to run application logic on a message.
        /// </summary>
        /// <param name="messageEnvelope"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<bool> HandleAsync(MessageEnvelope<T> messageEnvelope, CancellationToken token = default(CancellationToken));
    }
}
