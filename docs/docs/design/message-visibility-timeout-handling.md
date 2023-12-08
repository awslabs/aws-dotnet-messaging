# SQS Message Visibility Timeout Handling

When a consumer receives a message, Amazon SQS sets a [visibility timeout](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html). This is the period of time during which _additional_ consumers are prevented from receiving a duplicate message. The default visibility timeout is 30 seconds. The minimum is 0 seconds. The maximum is 12 hours. 

When a message handler returns `MessageProcessStatus.Success`, the framework will delete the message from the queue. When a handler returns `MessageProcessStatus.Failed`, the visibility timeout will be allowed to expire and the message will become visible to consumers again (pending interactions with other SQS settings, such as the message retention period or dead-letter queue maximum receives count).

The message processing framework tracks _in flight_ messages, which are those that have been received from the SQS queue but have not yet finished processing. The framework will periodically extend the visibility timeout of these messages to prevent another consumer from receiving them.

This visibility timeout extension behavior can be controlled by the following settings:
```
builder.AddSQSPoller("<queueURL>", options => 
{ 
    options.VisibilityTimeout = 30; 
    options.VisibilityTimeoutExtensionThreshold = 5;
    options.VisibilityTimeoutExtensionHeartbeatInterval = 1;
});
```
1. `VisibilityTimeout` - This is is the length of time in seconds that the message will not be visible to other consumers once it is received, as well as the length of time that the framework will extend the visibility timeout for messages that are still processing. The default value is 30 seconds.
    * Note that the SQS poller will always set this when it receives messages with either the configured value or _framework default_ of 30 seconds, it will not respect the value configured on the queue.
2. `VisibilityTimeoutExtensionThreshold` - When an in flight message is within this many seconds of becoming visible again, the framework will extend its visibility timeout automatically. The new visibility timeout will be set to `VisibilityTimeout` relative to now. The default value is 5 seconds.
3. `VisibilityTimeoutExtensionHeartbeatInterval` - This is how frequently the framework will check in flight messages and extend the the visibility timeout of messages that are within the `VisibilityTimeoutExtensionThreshold`. The default value is 1 second.

Refer to [processing messages in a timely manner](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/working-with-messages.html#processing-messages-timely-manner) for possible strategies for these options.

As an example for a message handler that takes 45 seconds to complete and the options above:
* T+0: The message is received from SQS
* T+1 through T+24: The heartbeat task will check the message every second, but since it is not within 5 seconds of becoming visible again it will do nothing.
* T+25: The heartbeat will see that the message is within the 5 second threshold of becoming visible again, so it will change the visibility timeout to 30 seconds from now (T+55).
* T+25 through T+44: The heartbeat task will check the message every second, but since it is not within 5 seconds of becoming visible again it will do nothing.
* T+45: The message handler completes, so the framework will delete the message from the queue.

The heartbeat task will check _all_ in flight messages (the number of which are controlled by `options.MaxNumberOfConcurrentMessages`) at each heartbeat interval and batch the extensions together for messages within the threshold via [ChangeMessageVisibilityBatch](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_ChangeMessageVisibilityBatch.html).

The message visibility timeout extension feature is only available to the SQS poller configured via `AddSQSPoller`, and **not** to the forthcoming Lambda message pump. When [using Lambda with Amazon SQS](https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html), Lambda will use the queue's configured visibility timeout, and [recommends setting that to at least six times the configured function timeout](https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html#events-sqs-queueconfig). 

Open Questions:
1. When a handler returns `MessageProcessStatus.Failed`, should the framework let the remaining visibility timeout run out before the message becomes visible again? Or should the framework call `ChangeMessageVisibility` with 0 seconds so the message becomes available again immediately? 
    * Perhaps this should be configurable as a boolean? `options.MakeFailedMessagesVisibleImmediately`?
    * Or the result of different `MessageProcessStatus` values that a message handler could return? 
2. Should the `SQSPoller` give users the ability to opt-out of the automatic visibility timeout extensions?
    * Perhaps by setting `VisibilityTimeoutExtensionHeartbeatInterval` to less than or equal to `0`? 
    * Or we can add an explicit `options.DisableVisibilityTimeoutExtensionHeartbeat` boolean?