using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Rebus;
using Rebus.Extensions;
using Rebus.Configuration;

namespace Rebus.LegacySample
{
	public static class SubscriptionExtensions
	{
		private static MethodInfo _busSubscribeMethod = typeof(IBus).GetMethod("Subscribe");

		private static IEnumerable<Type> GetMensajesHandledBy(Assembly assembly)
		{
			return assembly
					.ExportedTypes
					.SelectMany(x => x.GetInterfaces())
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
					.Select(x => x.GetGenericArguments()[0])
					.ToArray();
		}

		public static void SubscribeFrom(this IBus bus, params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
			{
				foreach (var msg in GetMensajesHandledBy(assembly))
				{
					var _subscribeMethod = _busSubscribeMethod.MakeGenericMethod(msg);
					_subscribeMethod.Invoke(bus, new object[] { });
				}
			}
		}

		public static void SubscribeFromCurrentAssembly(this IBus bus)
		{
			bus.SubscribeFrom(Assembly.GetCallingAssembly());
		}
	}
}
