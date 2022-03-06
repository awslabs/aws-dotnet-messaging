using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Amazon.SQS.Model;

namespace AWS.MessageProcessing.Serialization
{
    /// <summary>
    /// Used for serializing and deserializing SQS messages into message envelopes.
    /// </summary>
    public interface IEnvelopeSerialization
    {
        /// <summary>
        /// Serialize the MessageEnvelope<T> to the SQS message format that will be sent to an SQS queue by the framework.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        Message Serialize<T>(MessageEnvelope<T> message);

        /// <summary>
        /// Deserialize the SQS message into the intermediate FlatMessageEnvelope type that framework will later use to determine the .NET type and convert to MessageEnvelope<T>.
        /// </summary>
        /// <param name="sqsMessage"></param>
        /// <returns></returns>
        FlatMessageEnvelope Deserialize(Message sqsMessage);
    }

    /// <summary>
    /// Default implementation of IEnvelopeSerialization. This implementation assumes the format of the message to have an outer later JSON document 
    /// that has envelope properties including a "MessageType" used to indicate the type of message. The contents of the application message
    /// are in the "Message" property. The contents of the "Message" property are not processed in the IEnvelopeSerialization and instead
    /// parsed later in the process by the registered IMessageSerialization.
    /// 
    /// Here is an example of a message:
    /// <code>
    /// {
    ///   "Id": "1234",
    ///   "Source": "OrderIntakeLambdaFunction",
    ///   "CreatedTimeStamp": "2022-03-05T23:40:34.6703311+00:00",
    ///   "MessageType": "CommonModels.OrderInfo",
    ///   "Message": {
    ///     "OrderId": "oid",
    ///     "UserId": "uid",
    /// 	"Items" : [
    /// 		    {
    /// 			    "ProductId" : "pid",
    /// 			    "Count" : 3
    ///             }
    /// 	    ]
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class DefaultEnvelopeSerialization : IEnvelopeSerialization
    {
        /// <inheritdoc/>
        public FlatMessageEnvelope Deserialize(Message sqsMessage)
        {
            // TODO: Handle message attributes

            var messageData = sqsMessage.Body;

            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(messageData);
            }
            catch(JsonException e)
            {
                throw new InvalidMessageFormatException("Unable to parse envelope message", messageData, e);
            }

            var envelope = new FlatMessageEnvelope();
            if(!jsonDoc.RootElement.TryGetProperty(nameof(FlatMessageEnvelope.Id), out var id))
            {
                throw new InvalidMessageFormatException($"Envelope message missing {nameof(FlatMessageEnvelope.Id)} property", messageData);
            }
            envelope.Id = id.GetString();

            if (jsonDoc.RootElement.TryGetProperty(nameof(FlatMessageEnvelope.Source), out var source))
            {
                envelope.Source = source.GetString();
            }

            if (!jsonDoc.RootElement.TryGetProperty(nameof(FlatMessageEnvelope.CreatedTimeStamp), out var createdTimeStampStr))
            {
                throw new InvalidMessageFormatException($"Envelope message missing {nameof(FlatMessageEnvelope.CreatedTimeStamp)} property", messageData);
            }
            if(!DateTime.TryParse(createdTimeStampStr.GetString(), out var createdTimeStamp))
            {
                throw new InvalidMessageFormatException($"Unable to parse timestamp for CreatedTimeStamp property: {createdTimeStampStr}", messageData);
            }
            envelope.CreatedTimeStamp = createdTimeStamp;

            if (!jsonDoc.RootElement.TryGetProperty(nameof(FlatMessageEnvelope.MessageType), out var messageType))
            {
                throw new InvalidMessageFormatException($"Envelope message missing {nameof(FlatMessageEnvelope.MessageType)} property", messageData);
            }
            envelope.MessageType = messageType.GetString();


            if (!jsonDoc.RootElement.TryGetProperty("Message", out var rawMessage))
            {
                throw new InvalidMessageFormatException($"Envelope message missing Message property", messageData);
            }
            envelope.RawMessage = rawMessage.GetRawText();

            return envelope;
        }

        /// <inheritdoc/>
        public Message Serialize<T>(MessageEnvelope<T> message)
        {
            throw new NotImplementedException();
        }
    }
}
