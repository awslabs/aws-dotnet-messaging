using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.MessageProcessing.MessagePump
{
    public interface IMessageManager
    {
        public int ActiveMessageCount { get; }

        public void ProcessMessage(MessageEnvelope messageEnvelope);
    }

    public interface IMessageManagerFactory
    {
        IMessageManager CreateMessageManager(IMessageReader reader);
    }

    public class DefaultMessageManagerFactory : IMessageManagerFactory
    {
        IServiceProvider _serviceProvider;

        public DefaultMessageManagerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMessageManager CreateMessageManager(IMessageReader reader)
        {
            return ActivatorUtilities.CreateInstance<IMessageManager>(_serviceProvider, reader);
        }
    }

    //public class DefaultMessageManager : IMessageManager
    //{

    //}
}
