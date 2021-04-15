using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.LegacyExtensions
{
	/* When using OneExchangePerClassPublish in proyects with modern Rebus, for compatibility, when publishing 2 messages are sent:
	1. Message sent to the Exchange with the same ClassName.
	2. Message sent to the new rebus topic exchange.
	For avoiding duplicated messages in modern rebus with OneExchangePerClassPublish activated in this step we ignore one of the messages.
	The ignored message is determined by the header 'rbs2-rebus-legacy-compatibility' that is included in 'RabbitMqEnhacedTransport'
	*/
	public class DeleteDuplicatedLegacyMessagesIncomingStep : IIncomingStep
	{
		private readonly ILog _log;

		public DeleteDuplicatedLegacyMessagesIncomingStep(IRebusLoggerFactory rebusLoggerFactory)
		{
			if (rebusLoggerFactory == null)
				throw new ArgumentNullException("rebusLoggerFactory cannot be null");

			_log = rebusLoggerFactory.GetLogger<DeleteDuplicatedLegacyMessagesIncomingStep>();
		}

		private bool DiscardLegacyDuplicatedMessages(TransportMessage message)
		{
			if (message.Headers.ContainsKey("rbs2-rebus-legacy-compatibility"))
				return true;
			else
				return false;
		}

		public async Task Process(IncomingStepContext context, Func<Task> next)
		{
			var transportMessage = context.Load<TransportMessage>();

			if (DiscardLegacyDuplicatedMessages(transportMessage))
			{
				_log.Debug($"Ignoring duplicated message with id {transportMessage.Headers[Headers.MessageId]} " +
					$"and type: {transportMessage.Headers[Headers.Type]}");
				MessageContext.Current.AbortDispatch();
			}

			await next();
		}
	}
}