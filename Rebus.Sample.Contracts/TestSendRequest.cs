using System;

namespace Rebus.Sample.Contracts
{
	public class TestSendRequest
	{
		public Guid MessageId { get; set; }
		public string MessageContent { get; set; }

		public DateTime MessageDate { get; set; }
	}
}
