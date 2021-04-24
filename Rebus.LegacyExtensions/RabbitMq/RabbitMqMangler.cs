using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Rebus.Transport;
using Rebus.Logging;
using Rebus.Subscriptions;

namespace Rebus
{
	internal class RabbitMqMangler : IDisposable
	{
		private readonly ILog _log;
		private readonly Type _transportType;
		private readonly ITransport _transport;
		private readonly object _connectionManager;

		#region Reflection Code..

		private Lazy<FieldInfo> ConnectionManagerField => new Lazy<FieldInfo>(
			() => GetTransportType("Rebus.RabbitMq.RabbitMqTransport")
				.GetField("_connectionManager", BindingFlags.NonPublic | BindingFlags.Instance)
		);
		private Lazy<MethodInfo> GetConnectionMethod => new Lazy<MethodInfo>(
			() => GetTransportType("Rebus.Internals.ConnectionManager").GetMethod("GetConnection")
		);

		private object GetConnection() => GetConnectionMethod.Value.Invoke(_connectionManager, new object[] { });

		private readonly Assembly RabbitMqAssembly = GetRabbitMqAssembly();

		private Lazy<MethodInfo> CreateModelMethod => new Lazy<MethodInfo>(
			() => GetRabbitType("RabbitMQ.Client.IConnection").GetMethod("CreateModel")
		);

		private Lazy<MethodInfo> ExchangeDeclareMethod => new Lazy<MethodInfo>(
			() => GetRabbitType("RabbitMQ.Client.IModel").GetMethod("ExchangeDeclare")
		);

		private Lazy<MethodInfo> QueueDeclareMethod => new Lazy<MethodInfo>(
			() => GetRabbitType("RabbitMQ.Client.IModel").GetMethod("QueueDeclare")
		);

		private Lazy<MethodInfo> QueueBindMethod => new Lazy<MethodInfo>(
			() => GetRabbitType("RabbitMQ.Client.IModel").GetMethod("QueueBind")
		);

		private Lazy<MethodInfo> ExchangeBindMethod => new Lazy<MethodInfo>(
			() => GetRabbitType("RabbitMQ.Client.IModel").GetMethod("ExchangeBind")
		);

		private Lazy<MethodInfo> ExchangeUnbindMethod => new Lazy<MethodInfo>(
			() => GetRabbitType("RabbitMQ.Client.IModel").GetMethod("ExchangeUnbind")
		);
		#endregion

		public RabbitMqMangler(ITransport transport, IRebusLoggerFactory rebusLoggerFactory)
		{
			transport = GetActualRabbitTransport(transport);

			if (rebusLoggerFactory == null)
				throw new ArgumentNullException("rebusLoggerFactory cannot be null");

			if (transport == null)
				throw new ArgumentNullException("transport cannot be null");

			EnsureIsRabbitMqTransport(transport);

			_transport = transport;
			_transportType = transport.GetType();
			_connectionManager = ConnectionManagerField.Value.GetValue(transport);
			_log = rebusLoggerFactory.GetLogger<RabbitMqMangler>();
		}

		internal static void EnsureIsRabbitMqTransport(ITransport transport)
		{
			if (transport.GetType().FullName != "Rebus.RabbitMq.RabbitMqTransport")
				throw new ArgumentException($"Cannot initialize RabbitMqMangler, transportType is {transport.GetType()} and not RabbitMqTransport");
		}

		private Type GetTransportType(string name)
		{
			return _transportType.Assembly.GetType(name, true);
		}

		private Type GetRabbitType(string name)
		{
			return RabbitMqAssembly.GetType(name, true);
		}

		private static Assembly GetRabbitMqAssembly()
		{
			var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "RabbitMQ.Client");

			if (assembly == null)
				throw new ArgumentNullException("Required assembly 'RabbitMQ.Client' cannot be obtained");

			return assembly;
		}

		private ITransport GetActualRabbitTransport(ITransport transport)
		{
			switch (transport)
			{
				case RabbitMqEnhancedTransport t: return t.InnerTransport;
				default: return transport;
			}
		}

		private IDisposable CreateModelFrom(object connection)
		{
			_log.Info("Creating RabbitMq model");
			return CreateModelMethod.Value.Invoke(connection, new object[] { }) as IDisposable;
		}

		public string GetTopicExchangeName()
		{
			var address = (_transport as ISubscriptionStorage).GetSubscriberAddresses("dummy").Result;

			if (address.Length < 1)
				throw new ArgumentException("'address' lenght is less than 1 ?!?!");
			if (!address[0].Contains("@"))
				throw new ArgumentException($"'address[0]' value should contain an '@', but his value is {address[0]}");

			return address[0].Split('@')[1];
		}

		public void DeclareQueue(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false)
		{
			_log.Info($"Declaring queue with name {queueName}");

			var connection = GetConnection();

			using (var model = CreateModelFrom(connection))
			{
				QueueDeclareMethod.Value.Invoke(model, new object[] { queueName, durable, exclusive, autoDelete, null});
			}
		}

		public void BindQueue(string queueName, string exchangeName)
		{
			_log.Info($"Binding queue{exchangeName} to {queueName}");

			var connection = GetConnection();

			using (var model = CreateModelFrom(connection))
			{
				QueueBindMethod.Value.Invoke(model, new object[] {queueName, exchangeName, "", null});
			}
		}

		public void BindExchange(string destination, string source, string routingKey = "")
		{
			_log.Info($"Binding exchange {source} to {destination}");

			var connection = GetConnection();

			using (var model = CreateModelFrom(connection))
			{
				ExchangeBindMethod.Value.Invoke(model, new object[] { destination, source, routingKey, null });
			}
		}

		public void UnbindExchange(string destination, string source, string routingKey = "")
		{
			_log.Info($"Unbinding exchange {source} to {destination}");

			var connection = GetConnection();

			using (var model = CreateModelFrom(connection))
			{
				ExchangeUnbindMethod.Value.Invoke(model, new object[] { destination, source, routingKey, null });
			}
		}

		public void DeclareExchange(string exchangeName)
		{
			var connection = GetConnection();

			using (var model = CreateModelFrom(connection))
			{
				_log.Info($"Declaring exchange with name: {exchangeName}");
				ExchangeDeclareMethod.Value.Invoke(model, new object[] {exchangeName, "fanout", true, false, null});
			}
		}

		public void Dispose()
		{
			// No need to dispose _connectionManager
			// as their lifecycle is managed by transport.
		}
	}
}
