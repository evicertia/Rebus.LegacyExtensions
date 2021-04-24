using System;
using System.Text;
using System.Reflection;

using Castle.Windsor;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;

using Rebus;
using Rebus.Async;
using Rebus.Configuration;
using Rebus.Castle.Windsor;
using Rebus.RabbitMQ;

using HermaFx.Rebus;

namespace Rebus.LegacySample.Startup
{
	public class RebusFacility : AbstractFacility
	{
		#region Common Fields
		private readonly IWindsorContainer _container;
		#endregion

		public RebusFacility(IWindsorContainer container)
		{
			//_Log = logger;
			_container = container;
		}

		protected override void Init()
		{

			var brokerAddress = "amqp://guest:guest@127.0.0.1:5675/sample";

			var bus = Configure.With(new WindsorContainerAdapter(_container))
				.SetDateOnSentMessages()
				.ValidateOutgoingMessages()
				.ValidateIncomingMessages()
				.UseTimeoutAttribute()
				.Transport(t =>
				{
					var prefetch = (ushort?)null;
					var transport = t.UseRabbitMqAndGetInputQueueNameFromAppConfig(brokerAddress)
										.UseOneExchangePerMessageTypeRouting()
										.ManageSubscriptions();

					if (prefetch != null)
					{
						transport.SetPrefetchCount(prefetch.Value);
					}
				})
				.LogOutgoingMessages()
				.MessageOwnership(o => o.FromRebusConfigurationSection())
				.Serialization(s => s.UseJsonSerializer().SpecifyEncoding(Encoding.UTF8).SerializeEnumAsStrings(false))
				.EnableInlineReplyHandlers()
				.Behavior(b => b.SetMaxRetriesFor<Exception>(3))
				.CreateBus();

			Kernel.Register(Component.For<IBus, IStartableBus>().Instance((IBus)bus));
			((IBus)bus).SubscribeFromCurrentAssembly();

			var bus2 = Kernel.Resolve<IBus>();
			(bus2 as IStartableBus).Start(5);

			//AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;

		}
	}
}
