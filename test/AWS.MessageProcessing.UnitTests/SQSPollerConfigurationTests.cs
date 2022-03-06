namespace AWS.MessageProcessing.UnitTests
{
    public class SQSPollerConfigurationTests
    {
        [Fact]
        public void CreateSQSPollerConfiguration()
        {
            var queueUrl = "https://sqs.us-west-2.amazonaws.com/123412341234/my-queue";
            var config = new SQSPollerConfiguration(queueUrl);
            Assert.Equal(queueUrl, config.QueueUrl);
        }
    }
}
