using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Rebus.Logging;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus
{
	public class RabbitMqEnhancedTransport : ITransport, IDisposable
	{
		public readonly ITransport _inner;
		private readonly RabbitMqMangler _mangler;
		private readonly ILog _log;
		private readonly string _topicExchangeName;

		internal ITransport InnerTransport => _inner;

		public RabbitMqEnhancedTransport(ITransport inner, IRebusLoggerFactory rebusLoggerFactory)
		{
			if (rebusLoggerFactory == null)
				throw new ArgumentNullException("rebusLoggerFactory cannot be null");

			_inner = inner ?? throw new ArgumentNullException("inner transport cannot be null");
			_log = rebusLoggerFactory.GetLogger<RabbitMqEnhancedTransport>();
			_mangler = new RabbitMqMangler(inner, rebusLoggerFactory);
			_topicExchangeName = _mangler.GetTopicExchangeName();
		}

		public string Address => _inner.Address;

		private bool IsPublish(TransportMessage message)
		{
			var intent = message.Headers["rbs2-intent"];
			return intent == "pub";
		}

		// Returns class exchange name if message is a publish. Address comes in format 'ClassName, queue@exchange'.
		// For verifing publish, address should end with '@TopicQueue' and have two parts separated with ','.
		private bool AddressContainsTypeName(string address, out string typeName)
		{
			var topicExchange = _topicExchangeName;
			var splitedAddress = address.Split(',');

			if(address.EndsWith($"@{topicExchange}") && splitedAddress.Length == 2)
			{
				typeName = splitedAddress[0];
				return true;
			}
			else
			{
				typeName = null;
				return false;
			}
		}

		public void CreateQueue(string address)
		{
			_inner.CreateQueue(address);
		}

		// Add header for verify if it comes from v2
		// Ignore messages duplicated based in the header
		public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
		{
			var message = await _inner.Receive(context, cancellationToken);

			return message;
		}

		// In case of publish the send is made in the usual way and to his own class exchange depending of the message type
		public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
		{
			_log.Info($"Sending message with id: {message.Headers["rebus-msg-id"]}");

			await _inner.Send(destinationAddress, message, context);

			if (IsPublish(message) && AddressContainsTypeName(destinationAddress, out var typeName))
			{
				var legacyMessage = new TransportMessage(new Dictionary<string, string>(message.Headers), message.Body);
				legacyMessage.Headers.Add("rbs2-rebus-legacy-compatibility", null);
				_mangler.DeclareExchange(typeName);
				await _inner.Send("@" + typeName, legacyMessage, context);
			}
		}

		public void Dispose()
		{
			// No need to dispose _inner, nor _transport
			// as their lifecycle is managed by rebus.
		}
	}
}
