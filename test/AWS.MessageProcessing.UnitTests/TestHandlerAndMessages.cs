using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.MessageProcessing.UnitTests
{
    public class FooBarMessage
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }

    public class FooBarHandler : IMessageHandler<FooBarMessage>
    {
        public Task<bool> HandleAsync(MessageEnvelope<FooBarMessage> messageEnvelope, CancellationToken token = default)
        {
            if (string.Equals(messageEnvelope.Id, "error", StringComparison.OrdinalIgnoreCase))
                throw new ApplicationException("Simulated Error");

            return Task.FromResult(true);
        }
    }
}
