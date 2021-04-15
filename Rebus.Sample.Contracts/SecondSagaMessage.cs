using System;

namespace Rebus.Sample.Contracts
{
	public class SecondSagaMessage : IMessageWithUserId
	{
		public Guid UserId { get; set; }
		public DateTime CreationDate { get; set; }
		public string UserName { get; set; }

		public bool Confirmed { get; set; }
	}
}
