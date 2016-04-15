namespace Gambit.Client.Model
{
	using System;
	using Data.Response;

	public class RecievedMessage
    {
        public DateTime DateRecieved { get; set; }
        public MessageResponse Message { get; set; }
    }
}
