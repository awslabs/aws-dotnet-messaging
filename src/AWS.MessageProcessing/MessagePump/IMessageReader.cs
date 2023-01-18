using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AWS.MessageProcessing.MessagePump;

public interface IMessageReader
{
    public Task StartReaderAsync(CancellationToken token = default(CancellationToken));

    public Task DeleteMessagesAsync(IEnumerable<MessageEnvelope> messages);

    public Task ExtendMessageVisiblityAsync(IEnumerable<MessageEnvelope> messages);
}

public interface IMessageReaderFactory
{
    public IMessageReader CreateMessageReader(string resourceType);
}

internal class DefaultMessageReaderFactory : IMessageReaderFactory
{
    private IServiceProvider _serviceProvider;

    public DefaultMessageReaderFactory(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    public IMessageReader CreateMessageReader(string resourceType)
    {
        var reader = ActivatorUtilities.CreateInstance<SQSMessageReader>(_serviceProvider);
        return reader;
    }
}