using System;
using Rebus.Config;
using Rebus.ServiceProvider;
using Rebus;
using Rebus.Routing.TypeBased;

using Rebus.LegacyExtensions;
using Rebus.Retry.Simple;
using System.Text;
using Rebus.Persistence.InMem;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class BrokerConfigExtensions
	{
		public static IServiceCollection AddBroker(
				 this IServiceCollection services)
		{
			AppContext.SetSwitch(
				"System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

			services.AddRebus(configure => configure
				.Transport(t =>
				{
					t.UseRabbitMq("amqp://guest:guest@127.0.0.1:5675/sample", "Rebus.Server:MailBox")
						.ExchangeNames(directExchangeName: "Rebus:Server", topicExchangeName: "RebusTest");
					t.UseOneExchangePerClassPublish("RebusClassExchange:Server");
				}
				)
				.Options(o =>
				{
					o.EnableLegacyCompatibility(Encoding.UTF8);
					o.EnableSynchronousRequestReply();
					o.SimpleRetryStrategy(errorQueueAddress: "Rebus.Server::Bounces");
				})
				.Sagas(s => s.StoreInMemory())
				.Routing(r => r.TypeBasedRoutingFromAppConfig())
				);

			return services;
		}
	}
}