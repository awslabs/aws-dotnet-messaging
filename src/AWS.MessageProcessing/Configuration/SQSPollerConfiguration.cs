namespace AWS.MessageProcessing.Configuration
{
    /// <summary>
    /// Container for the configuration to poll a SQS queue.
    /// 
    /// TODO: Add properties for configuring message visiblity, parallelization, MaxNumberOfMessages
    /// </summary>
    public class SQSPollerConfiguration
    {
        /// <summary>
        /// The SQS QueueUrl to poll messages from.
        /// </summary>
        public string QueueUrl { get; }

        /// <summary>
        /// Construct an instance of SQSPollerConfiguration
        /// </summary>
        /// <param name="queueUrl"></param>
        public SQSPollerConfiguration(string queueUrl)
        {
            this.QueueUrl = queueUrl;
        }
    }
}
