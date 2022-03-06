using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.MessageProcessing
{
    /// <summary>
    /// Non generic base class for MessageEnvelope objects.
    /// </summary>
    public abstract class MessageEnvelope
    {
        // Disable nullablity check because this all of these values are required but are set via deserialization.
#pragma warning disable CS8618
        public string Id { get; set; }

        public string? Source { get; set; }

        public DateTime CreatedTimeStamp { get; set; }

        public string MessageType { get; set; }
#pragma warning restore CS8618

        protected internal abstract void SetMessage(object message);
    }

    /// <summary>
    /// Container class for messages being processed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessageEnvelope<T> : MessageEnvelope
    {
        // Disable nullablity check because this all of these values are required but are set via deserialization.
#pragma warning disable CS8618

        /// <summary>
        /// The application message that will be processed.
        /// </summary>
        public T Message { get; set; }
#pragma warning restore CS8618

        /// <summary>
        /// Method used by the framework to convert from the FlatMessageEnvelope to the generic typed MessageEnvelope
        /// </summary>
        /// <param name="message"></param>
        protected internal override void SetMessage(object message)
        {
            this.Message = (T)message;
        }
    }
}
