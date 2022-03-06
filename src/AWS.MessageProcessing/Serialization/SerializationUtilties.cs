using AWS.MessageProcessing.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.MessageProcessing.Serialization
{
    /// <summary>
    /// Utility to SQS Message to MessageEnvelope<T> using the registered IEnvelopeSerialization and IMessageSerialization
    /// </summary>
    public class SerializationUtilties
    {
        IServiceProvider _serviceProvider;
        IEnvelopeSerialization _envelopeSerializer;
        IMessageSerialization _messageSerializer;
        IMessagingConfiguration _messageConfiguration;

        /// <summary>
        /// Constructs an instance of SerializationUtilties
        /// </summary>
        /// <param name="serviceProvider">The service provider used to create instances of .NET message type</param>
        /// <param name="envelopeSerializer">The serializer for the envelope information part of a message.</param>
        /// <param name="messageSerializer">The serializer for the application message.</param>
        /// <param name="messageConfiguration">The message configuration used to look up message type mapping.</param>
        public SerializationUtilties(IServiceProvider serviceProvider, IEnvelopeSerialization envelopeSerializer, IMessageSerialization messageSerializer, IMessagingConfiguration messageConfiguration)
        {
            _serviceProvider = serviceProvider;
            _envelopeSerializer = envelopeSerializer;
            _messageSerializer = messageSerializer;
            _messageConfiguration = messageConfiguration;
        }

        /// <summary>
        /// Converts the SQS message to MessageEnvelope<T>.
        /// </summary>
        /// <param name="message">The SQS message</param>
        /// <returns>The MessageEnvelope<T> and the handler mapping the framework can used to send the message to the handler.</returns>
        /// <exception cref="InvalidMessageFormatException"></exception>
        /// <exception cref="FatalErrorException"></exception>
        public ConvertResults ConvertToEnvelopeMessage(Message message)
        {
            // Deserialize the envelope part of the message.
            FlatMessageEnvelope flatMessageEnvelope;
            try
            {
                flatMessageEnvelope = _envelopeSerializer.Deserialize(message);
            }
            catch (InvalidMessageFormatException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new InvalidMessageFormatException($"Unknown error parsing message envelope: {e.Message}", message.Body, e);
            }

            // Find the message type mapping for the message.
            var mapping = _messageConfiguration.GetHandlerMapping(flatMessageEnvelope.MessageType);
            if (mapping == null)
            {
                throw new InvalidMessageFormatException($"Failed to find message mapping for message type {flatMessageEnvelope.MessageType}", message.Body);
            }

            // Deserialize the application message into the .NET type registered for the message type.
            object messageObject;
            try
            {
                messageObject = _messageSerializer.Deserialize(flatMessageEnvelope.RawMessage, mapping.MessageType);
            }
            catch (Exception e)
            {
                throw new InvalidMessageFormatException($"Unknown error parsing message body: {e.Message}", flatMessageEnvelope.RawMessage, e);
            }

            // Create the new Generic MessageEnvelope<T> now that we know the type to create.
            var envelopeMessageType = typeof(MessageEnvelope<>).MakeGenericType(mapping.MessageType);
            MessageEnvelope? envelopeMessage = ActivatorUtilities.CreateInstance(_serviceProvider, envelopeMessageType) as MessageEnvelope;

            // This should never happen so if it does there we should fail hard because something must be seriously wrong.
            if (envelopeMessage == null)
            {
                throw new FatalErrorException($"Failed to create envelop message type {envelopeMessageType.FullName}");
            }

            // Copy all of the information from the intermediate FlatMessageEnvelope to the real MessageEnvelope<T> type
            // used by the users IMessageHandler.
            envelopeMessage.Id = flatMessageEnvelope.Id;
            envelopeMessage.CreatedTimeStamp = flatMessageEnvelope.CreatedTimeStamp;
            envelopeMessage.Source = flatMessageEnvelope.Source;
            envelopeMessage.MessageType = flatMessageEnvelope.MessageType;
            envelopeMessage.SetMessage(messageObject);

            return new ConvertResults(envelopeMessage, mapping);
        }

        /// <summary>
        /// Container class containing both the MessageEnvelope and handler mapping used by the framework 
        /// to send the message to the appropiate user code.
        /// </summary>
        public class ConvertResults
        {
            public MessageEnvelope MessageEnvelope { get; }
            public HandlerMapping Mapping { get; }

            public ConvertResults(MessageEnvelope messageEnvelope, HandlerMapping mapping)
            {
                this.MessageEnvelope = messageEnvelope;
                this.Mapping = mapping;
            }
        }
    }
}
