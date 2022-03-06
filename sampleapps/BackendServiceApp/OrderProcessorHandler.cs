using AWS.MessageProcessing;
using CommonModels;
using Microsoft.Extensions.Hosting;

namespace BackendServiceApp
{
    public class OrderProcessorHandler : IMessageHandler<OrderInfo>
    {
        public Task<bool> HandleAsync(MessageEnvelope<OrderInfo> messageEnvelope, CancellationToken token = default)
        {
            if(messageEnvelope.Message.OrderId == "nan")
            {
                throw new ApplicationException("Invalid order id");
            }

            Console.WriteLine("Processing Order: " + messageEnvelope.Message.OrderId);
            return Task.FromResult(true);
        }
    }
}
