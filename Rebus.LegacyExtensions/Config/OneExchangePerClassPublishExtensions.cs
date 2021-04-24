using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rebus;
using Rebus.LegacyExtensions;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus.Config
{
	public static class OneExchangePerClassPublishExtensions
	{
		public static void UseOneExchangePerClassPublish(this StandardConfigurer<ITransport> configurer, string exchangeName)
		{
			if (configurer == null)
				throw new ArgumentNullException("Configurer cannot be null");

			// Configure as https://github.com/rebus-org/Rebus.RabbitMq/blob/f34cf6b5b203d9b6d708db0f8937f928b297ab2e/Rebus.RabbitMq/Config/RabbitMqConfigurationExtensions.cs build internal

			configurer.Decorate(c =>
			{
				var transport = c.Get<ITransport>();

				RabbitMqMangler.EnsureIsRabbitMqTransport(transport);
				return new RabbitMqEnhancedTransport(transport, c.Get<IRebusLoggerFactory>());
			});


			configurer.OtherService<ISubscriptionStorage>().Decorate(c =>
			{
				return new RabbitMqEnhacedSubscriptionStorage(exchangeName,
					c.Get<ISubscriptionStorage>(), c.Get<ITransport>(), c.Get<IRebusLoggerFactory>());
			});

			configurer.OtherService<IPipeline>().Decorate(c =>
			{
				var pipeline = c.Get<IPipeline>();

				pipeline = new PipelineStepConcatenator(pipeline)
					.OnReceive(new DeleteDuplicatedLegacyMessagesIncomingStep(c.Get<IRebusLoggerFactory>()), PipelineAbsolutePosition.Front);

				return pipeline;
			});
		}
	}
}
