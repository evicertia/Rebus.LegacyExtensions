using System;

namespace Rebus.LegacySample.Contracts
{
	public class LegacySampleMessage
	{
		public Guid MessageId { get; set; }
		public string MessageContent { get; set; }

		public DateTime MessageDate { get; set; }
	}
}
