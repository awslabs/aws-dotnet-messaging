namespace AWS.MessageProcessing.UnitTests
{
    public class HandlerMappingTests
    {
        [Fact]
        public void CreateWithMessageTypeIdentifier()
        {
            var mapping = new HandlerMapping(typeof(double), typeof(string), "mid");
            Assert.Equal(typeof(double), mapping.HandlerType);
            Assert.Equal(typeof(string), mapping.MessageType);
            Assert.Equal("mid", mapping.MessageTypeIdentifier);
        }

        [Fact]
        public void CreateWithoutMessageTypeIdentifier()
        {
            var mapping = new HandlerMapping(typeof(double), typeof(string));
            Assert.Equal(typeof(double), mapping.HandlerType);
            Assert.Equal(typeof(string), mapping.MessageType);
            Assert.Equal("System.String", mapping.MessageTypeIdentifier);
        }
    }
}
