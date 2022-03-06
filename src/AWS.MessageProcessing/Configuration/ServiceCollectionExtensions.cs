using System;
using System.Collections.Generic;
using System.Text;

using AWS.MessageProcessing.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAWSMessageBus(this IServiceCollection services, Action<MessageBusBuilder> builder)
        {
            // TODO: Add mechanism to pull in all of the configuration from IConfiguration

            var busBuilder = new MessageBusBuilder();
            builder(busBuilder);
            busBuilder.Build(services);

            return services;
        }
    }
}
