using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.MessageProcessing.Serialization
{
    /// <summary>
    /// Intermediate message format used to parse the envelope message before the message type has been determined. 
    /// This class is only meant to be used inside the framework and will be converted to an MessageEnvelope<T> 
    /// once the message type has been determined.
    /// </summary>
    public class FlatMessageEnvelope : MessageEnvelope
    {
        // Disable nullablity check because this all of these values are required but are set via deserialization.
#pragma warning disable CS8618

        /// <summary>
        /// Holder for the message content while the message type is being determined.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("Message")]
        public string RawMessage { get; set; }
#pragma warning restore CS8618

        protected internal override void SetMessage(object message)
        {
            throw new NotImplementedException("This should not be called for FlatMessageEnvelope because there is no typed message class at this stage.");
        }
    }
}
