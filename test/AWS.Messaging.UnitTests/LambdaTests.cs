// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using AWS.Messaging.Lambda;
using AWS.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using static Amazon.Lambda.SQSEvents.SQSEvent;

namespace AWS.Messaging.UnitTests;

public class LambdaTests
{
    [Fact]
    public async Task OneSuccessfulMessage()
    {
        var successMessage = new SimulatedMessage
        {
            Id = "success-message-1"
        };

        var log = await Execute(new SimulatedMessage[] { successMessage });

        Assert.Contains(successMessage.Id, log.ToString());
    }

    [Fact]
    public async Task MessageHandlerReturnsFailedStatus()
    {
        var failedMessage = new SimulatedMessage
        {
            Id = "failed-message-1",
            ReturnFailedStatus = true
        };

        await Assert.ThrowsAsync<LambdaInvocationFailureException>(async () =>  await Execute(new SimulatedMessage[] { failedMessage }));
    }

    [Fact]
    public async Task ConcurrencyOne()
    {
        var slowMessage = new SimulatedMessage
        {
            Id = "slow-message",
            WaitTime = TimeSpan.FromSeconds(2)
        };

        var fastMessage = new SimulatedMessage
        {
            Id = "fast-message"
        };

        var log = await Execute(new SimulatedMessage[] { slowMessage, fastMessage }, maxNumberOfConcurrentMessages: 1);

        // Since only one message was done at a time the fast message should have its finish log message later in the
        // log because it wasn't able to jump ahead of the slow message
        var finishSlowMessagePos = FindFinishPos(log, slowMessage);
        var finishFastMessagePos = FindFinishPos(log, fastMessage);
        Assert.True(finishSlowMessagePos < finishFastMessagePos);
    }

    [Fact]
    public async Task ConcurrencyTwo()
    {
        var slowMessage = new SimulatedMessage
        {
            Id = "slow-message",
            WaitTime = TimeSpan.FromSeconds(2)
        };

        var fastMessage = new SimulatedMessage
        {
            Id = "fast-message"
        };

        var log = await Execute(new SimulatedMessage[] { slowMessage, fastMessage }, maxNumberOfConcurrentMessages: 2);

        // Since both messages ran in parallel the second message should be done first because
        // it runs faster. This is checked by checking where in the log the finish message is at.
        var finishSlowMessagePos = FindFinishPos(log, slowMessage);
        var finishFastMessagePos = FindFinishPos(log, fastMessage);
        Assert.True(finishFastMessagePos < finishSlowMessagePos);
    }

    [Fact]
    public async Task MessageHandlerReturnsFailedStatusBatchResponse()
    {
        var failedMessage = new SimulatedMessage
        {
            Id = "failed-message-1",
            ReturnFailedStatus = true
        };

        var tuple = await ExecuteWithBatchResponse(new SimulatedMessage[] { failedMessage });
        Assert.Single(tuple.batchResponse.BatchItemFailures);
        Assert.Equal("1", tuple.batchResponse.BatchItemFailures[0].ItemIdentifier);
    }

    private async Task<string> Execute(SimulatedMessage[] messages, int maxNumberOfConcurrentMessages = 1)
    {
        var provider = CreateServiceProvider(maxNumberOfConcurrentMessages: maxNumberOfConcurrentMessages);
        var sqsEvent = await CreateLambdaEvent(provider, messages);


        var logger = new TestLambdaLogger();
        var context = new TestLambdaContext()
        {
            Logger = logger
        };
        var lambdaMessaging = provider.GetRequiredService<ILambdaMessaging>();
        var lambdaFunction = new SimulatedLambdaFunction(lambdaMessaging);
        await lambdaFunction.FunctionHandler(sqsEvent, context);

        return logger.Buffer.ToString();
    }

    private async Task<(string log, SQSBatchResponse batchResponse)> ExecuteWithBatchResponse(SimulatedMessage[] messages, int maxNumberOfConcurrentMessages = 1)
    {
        var provider = CreateServiceProvider(maxNumberOfConcurrentMessages: maxNumberOfConcurrentMessages);
        var sqsEvent = await CreateLambdaEvent(provider, messages);


        var logger = new TestLambdaLogger();
        var context = new TestLambdaContext()
        {
            Logger = logger
        };
        var lambdaMessaging = provider.GetRequiredService<ILambdaMessaging>();
        var lambdaFunction = new SimulatedLambdaFunction(lambdaMessaging);
        var batchResponse = await lambdaFunction.FunctionHandlerWithBatchResponse(sqsEvent, context);

        return (logger.Buffer.ToString(), batchResponse);
    }

    private async Task<SQSEvent> CreateLambdaEvent(IServiceProvider provider,  params SimulatedMessage[] messages)
    {
        var envelopeSerializer = provider.GetRequiredService<IEnvelopeSerializer>();
        
        var sqsEvent = new SQSEvent()
        {
            Records = new List<SQSEvent.SQSMessage>()
        };

        for(int i = 1; i <= messages.Length; i++)
        {
            var messageEnvelope = await envelopeSerializer.CreateEnvelopeAsync(messages[i - 1]);
            var messageBody = await envelopeSerializer.SerializeAsync(messageEnvelope);

            var sqsMessage = new SQSEvent.SQSMessage
            {
                Body = messageBody,
                    
                MessageId = i.ToString(),
                ReceiptHandle = "fake-receipt-handle-" + i,
                AwsRegion = "us-west-2",
                EventSource = "aws:sqs",
                EventSourceArn = "arn:aws:sqs:us-west-2:123412341234:SimulatedMessage",
                MessageAttributes = new Dictionary<string, MessageAttribute>(),
                Attributes = new Dictionary<string, string>()
            };
            sqsEvent.Records.Add(sqsMessage);
        }

        return sqsEvent;
    }

    private IServiceProvider CreateServiceProvider(int maxNumberOfConcurrentMessages = 1)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<SimulatedMessage>("https://sqs.us-west-2.amazonaws.com/123412341234/SimulatedMessage");
            builder.AddMessageHandler<SimulatedMessageHandler, SimulatedMessage>();

            builder.AddLambdaMessageProcessor(options =>
            {
                options.MaxNumberOfConcurrentMessages = maxNumberOfConcurrentMessages;
            });
        });

        return services.BuildServiceProvider();
    }

    private int FindFinishPos(string log, SimulatedMessage message)
    {
        var token = "Finished handler: " + message.Id;
        return log.IndexOf(token);
    }
}

public class SimulatedLambdaFunction
{
    private readonly ILambdaMessaging _lambdaMessaging;

    public SimulatedLambdaFunction(ILambdaMessaging lambdaMessaging)
    {
        _lambdaMessaging = lambdaMessaging;
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        await _lambdaMessaging.ProcessLambdaEventAsync(evnt, context);
    }

    public async Task<SQSBatchResponse> FunctionHandlerWithBatchResponse(SQSEvent evnt, ILambdaContext context)
    {
        return await _lambdaMessaging.ProcessLambdaEventWithBatchResponseAsync(evnt, context);
    }
}

public class SimulatedMessage
{
    public string Id { get; set; }

    public bool ReturnFailedStatus { get; set; } = false;

    public TimeSpan WaitTime { get; set; }
}

public class SimulatedMessageHandler : IMessageHandler<SimulatedMessage>
{
    public readonly ILambdaContext _lambdaContext;
    public SimulatedMessageHandler(ILambdaContext lambdaContext)
    {
        _lambdaContext = lambdaContext;
    }

    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<SimulatedMessage> messageEnvelope, CancellationToken token = default)
    {
        var message = messageEnvelope.Message;
        try
        {
            _lambdaContext.Logger.LogInformation("Message Id: " + message.Id);

            await Task.Delay(message.WaitTime);
            if (message.ReturnFailedStatus)
            {
                return MessageProcessStatus.Failed();
            }

            return MessageProcessStatus.Success();
        }
        finally
        {
            _lambdaContext.Logger.LogInformation("Finished handler: " + message.Id);
        }
    }
}
