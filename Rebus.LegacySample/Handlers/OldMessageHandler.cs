using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rebus;
using Rebus.LegacySample.Contracts;

namespace Rebus.LegacySample
{
	public class OldMessageHandler : IHandleMessages<LegacySampleMessage>
	{
		private IBus _bus;

		public OldMessageHandler(IBus bus)
		{
			_bus = bus;
		}

		public void Handle(LegacySampleMessage message)
		{
			Console.WriteLine($"Received message, content: {message.MessageContent}");
		}
	}
}
