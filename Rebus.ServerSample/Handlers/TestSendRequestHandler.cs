using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Sample.Contracts;

namespace Rebus.ServerSample
{
	public class TestSendRequestHandler : IHandleMessages<TestSendRequest>
	{
		private IBus _bus;
		public TestSendRequestHandler(IBus bus)
		{
			_bus = bus;
		}

		public Task Handle(TestSendRequest message)
		{
			Console.WriteLine($"Receviced TestSendRequest message date: {message.MessageDate}");

			return Task.CompletedTask;
		}
	}
}
