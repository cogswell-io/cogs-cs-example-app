namespace Gambit.Client.Model
{
    using System;
    using Data.Response;

    /// <summary>
    /// Class, containing the message response record
    /// </summary>
	public class ReceivedMessage
    {
        /// <summary>
        /// Creates a new ReceivedMessage instance stamped with the current UTC Date 
        /// from a given <see cref="MessageResponse"/>
        /// </summary>
        public ReceivedMessage(MessageResponse message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(
                    "message", "Cannot create a ReceivedMessage instance from null");
            }

            this.DateReceived = DateTime.UtcNow;
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets the creation date of the object (the time the message is received)
        /// </summary>
        public DateTime DateReceived { get; set; }

        /// <summary>
        /// Gets or sets the actual message response
        /// </summary>
        public MessageResponse Message { get; set; }
    }
}
