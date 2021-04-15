using System;
using System.Threading.Tasks;

using Rebus.Bus;

using Rebus.Sample.Contracts;
using Rebus.LegacySample.Contracts;

namespace Rebus.ClientSample
{
	public class SendTestService : ISendTestService
	{
		private IBus _bus;


		public SendTestService(IBus bus)
		{
			_bus = bus;
		}

		private void SendSagaMessage()
		{
			var req = new FirstSagaMessage()
			{
				UserId = Guid.NewGuid(),
				CreationDate = DateTime.UtcNow,
				UserName = "sagaInit"
			};

			_bus.Send(req);
		}

		public async Task<bool> SendMessage()
		{
			try
			{
				var req = new TestSendRequest()
				{
					MessageId = Guid.NewGuid(),
					MessageContent = "Mensaje enviado desde proyecto RebusTest",
					MessageDate = DateTime.UtcNow
				};


				var oldReq = new LegacySampleMessage()
				{
					MessageId = Guid.NewGuid(),
					MessageContent = "Mensaje enviado desde proyecto RebusTest",
					MessageDate = DateTime.UtcNow
				};

				var reqSaga = new FirstSagaMessage()
				{
					UserId = Guid.NewGuid(),
					CreationDate = DateTime.UtcNow,
					UserName = "sagaInit"
				};

				//SendSagaMessage();
				//var res = await _bus.SendRequest<TestSendResponse>(reqSaga, null, TimeSpan.FromSeconds(30));
				await _bus.Publish(req);
				//_bus.Publish(req);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}

			Console.WriteLine("Terminado sin errores");

			return true;
		}
	}
}
