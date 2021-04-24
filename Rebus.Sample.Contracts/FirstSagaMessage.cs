using System;

namespace Rebus.Sample.Contracts
{
	public class FirstSagaMessage : IMessageWithUserId
	{
		public Guid UserId { get; set; }
		public DateTime CreationDate { get; set; }
		public string UserName { get; set; }
	}
}
