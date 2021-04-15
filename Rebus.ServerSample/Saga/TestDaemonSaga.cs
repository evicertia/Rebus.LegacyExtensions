using HermaFx;
using Rebus;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Sagas;
using Rebus.Sample.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.ServerSample
{
	public partial class TestDaemonSaga : Saga<TestSagaData>,
		IAmInitiatedBy<FirstSagaMessage>,
		IHandleMessages<SecondSagaMessage>
	{

		private IBus _bus;

		public TestDaemonSaga(IBus bus)
		{
			Guard.IsNotNull(bus, nameof(bus));

			_bus = bus;
		}
		protected override void CorrelateMessages(ICorrelationConfig<TestSagaData> config)
		{
			config.Correlate<FirstSagaMessage>(m => m.UserId, d => d.UserId);
			config.Correlate<SecondSagaMessage>(m => m.UserId, d => d.UserId);
		}

		private void InitializeSagaData(FirstSagaMessage message)
		{
			Data.UserId = message.UserId;
			Data.UserName = message.UserName;
			Data.CreationDate = message.CreationDate;
		}

		public Task Handle(FirstSagaMessage message)
		{
			InitializeSagaData(message);

			var newMsg = new SecondSagaMessage()
			{
				UserId = message.UserId,
				UserName = message.UserName,
				CreationDate = message.CreationDate,
				Confirmed = true
			};

			_bus.Send(newMsg);

			return Task.CompletedTask;
		}

		public Task Handle(SecondSagaMessage message)
		{

			Console.WriteLine(message.CreationDate);

			throw new NotImplementedException();
		}
	}
}
