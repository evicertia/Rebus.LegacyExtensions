using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Subscriptions;
using Rebus.Transport;

namespace Rebus
{
	// Modified Subscription storage for subscribing/unsubscribing in OneExchangePerClass model
	public class RabbitMqEnhacedSubscriptionStorage : ISubscriptionStorage, IInitializable, IDisposable
	{
		private readonly ISubscriptionStorage _inner;
		private readonly ILog _log;
		private readonly ITransport _transport;
		private readonly RabbitMqMangler _mangler;

		public string LocalExchangeName { get; private set; }

		#region .ctor

		public RabbitMqEnhacedSubscriptionStorage(string exchangeName, ISubscriptionStorage inner, ITransport transport, IRebusLoggerFactory rebusLoggerFactory)
		{
			if (rebusLoggerFactory == null)
				throw new ArgumentNullException("rebusLoggerFactory cannot be null");

			_log = rebusLoggerFactory.GetLogger<RabbitMqEnhacedSubscriptionStorage>();
			_inner = inner ?? throw new ArgumentNullException("inner subscriptionStorage cannot be null");
			_transport = transport ?? throw new ArgumentNullException("transport cannot be null");
			_mangler = new RabbitMqMangler(transport, rebusLoggerFactory);
			LocalExchangeName = exchangeName;
		}

		#endregion

		public bool IsCentralized => _inner.IsCentralized;

		// Returns the class name from the topic, as topic comes in format 'ClassName, Assembly'
		private string GetExchangeTypeName(string topic)
		{
			var splitedAddress = topic.Split(',');

			if (splitedAddress.Length == 2)
			{
				return splitedAddress[0];
			}
			else
			{
				throw new ArgumentException($"Provided topic: {topic} cannot be splitted to get ExchangeTypeName");
			}
		}
		public void Initialize()
		{
			_log.Info($"Declaring local exchange name: {LocalExchangeName}");
			_mangler.DeclareExchange(LocalExchangeName);

			_log.Info($"Binding queue: {_transport.Address} to exchange: {LocalExchangeName}");
			_mangler.BindQueue(_transport.Address, LocalExchangeName);
		}

		public async Task<string[]> GetSubscriberAddresses(string topic)
		{
			return await _inner.GetSubscriberAddresses(topic);
		}

		// Check old rabbit for normalization

		// Register subscription for OneExchangePerClass.
		// Create class exchange and bind it to the directQueue
		public async Task RegisterSubscriber(string topic, string subscriberAddress)
		{
			_log.Info($"Registering subscription for {topic} at {subscriberAddress}");

			var classExchange = GetExchangeTypeName(topic);

			_mangler.DeclareExchange(classExchange);
			_mangler.BindExchange(LocalExchangeName, classExchange);
			await _inner.RegisterSubscriber(topic, subscriberAddress);
		}

		// Unregister subscriber, unbinding the usual binds, and the OnExchangePerClass bind.
		public async Task UnregisterSubscriber(string topic, string subscriberAddress)
		{
			_log.Info($"Un-registering subscription for {topic} at {subscriberAddress}");

			var classExchange = GetExchangeTypeName(topic);

			_mangler.UnbindExchange(LocalExchangeName, classExchange);
			await _inner.UnregisterSubscriber(topic, subscriberAddress);
		}

		public void Dispose()
		{
			// No need to dispose _inner, nor _transport
			// as their lifecycle is managed by rebus.
		}
	}
}