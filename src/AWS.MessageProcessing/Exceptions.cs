using System;
using System.Collections.Generic;
using System.Text;

namespace AWS.MessageProcessing
{
    /// <summary>
    /// Thrown when there are errors parsing the message. If this error ocurs the error will be logged but the framework will not shutdown due to 
    /// bad data being sent into the framework.
    /// </summary>
    public class InvalidMessageFormatException : Exception
    {
        public InvalidMessageFormatException(string message, string invalidMessageBody, Exception? innerException = null) : base(message, innerException) 
        { 
            InvalidMessageBody = invalidMessageBody;
        }

        /// <summary>
        /// The message body of the message that had an error being processed.
        /// </summary>
        public string InvalidMessageBody { get; private set; }
    }

    /// <summary>
    /// If a FatalErrorException occurs happens there is a coding issue within the application and framework will shutdown if an exception ocurrs.
    /// </summary>
    public class FatalErrorException : Exception
    {
        public FatalErrorException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
