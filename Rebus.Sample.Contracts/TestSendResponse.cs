using System;
using System.Collections.Generic;
using System.Text;

namespace Rebus.Sample.Contracts
{
	public class TestSendResponse
	{
		public Guid MessageId { get; set; }
		public string MessageContent { get; set; }

		public bool Processed { get; set; }
	}
}