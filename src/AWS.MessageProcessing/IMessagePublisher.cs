using System;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.MessageProcessing
{
    /// <summary>
    /// An interface that application developers inject into their application to handle publishing messages.
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// Publish the message to the registered publisher for the message type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task PublishAsync<T>(T message, CancellationToken token = default(CancellationToken));
    }
}
