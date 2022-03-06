using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using AWS.MessageProcessing.Configuration;
using Microsoft.Extensions.Logging;

namespace AWS.MessageProcessing.MessagePump
{
    /// <summary>
    /// Service to run in long running console application that starts the message pump for SQS queues.
    /// </summary>
    internal class MessagePumpService : BackgroundService
    {
        ILogger<MessagePumpService> _logger;
        IMessagingConfiguration _messageConfiguration;
        IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructs an instance of MessagePumpService
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="logger"></param>
        /// <param name="messagingConfiguration"></param>
        public MessagePumpService(IServiceProvider serviceProvider, ILogger<MessagePumpService> logger, IMessagingConfiguration messagingConfiguration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _messageConfiguration = messagingConfiguration;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return RunSQSPollersAsync(stoppingToken);
        }

        /// <summary>
        /// Starts up SQS pollers for each configured queue and waits till of the poller tasks complete before exiting.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task RunSQSPollersAsync(CancellationToken stoppingToken)
        {
            var tasks = new List<Task>();

            if (_messageConfiguration.SQSPollerConfigurations != null)
            {
                foreach (var sqsPollerConfiguration in _messageConfiguration.SQSPollerConfigurations)
                {
                    var poller = ActivatorUtilities.CreateInstance<SQSPullMessagePump>(_serviceProvider, sqsPollerConfiguration);
                    tasks.Add(poller.RunAsync(stoppingToken));
                }
            }

            // TODO: Handle if a tasks fails how to restart. Unless any of the tasks fail with a FatalErrorException then shutdown the message pump.
            await Task.WhenAll(tasks);
        }
    }
}
