using System;
using Rebus.Config;
using Rebus.ServiceProvider;
using System.Text;

using Rebus.Retry.Simple;


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
				.Options(o =>
				{
					o.EnableLegacyCompatibility(Encoding.UTF8);
					o.SimpleRetryStrategy(errorQueueAddress: "Rebus.Client:Bounces");
					o.EnableSynchronousRequestReply();
				})
				.Routing(r => r.TypeBasedRoutingFromAppConfig())
				.Transport(t =>
				{
					t.UseRabbitMq("amqp://guest:guest@127.0.0.1:5675/sample", "Rebus.Client:MailBox")
						.ExchangeNames(directExchangeName: "Rebus:Client", topicExchangeName: "RebusTest");
					t.UseOneExchangePerClassPublish("RebusClassExchange:Client");
				})
			);

			return services;
		}
	}
}