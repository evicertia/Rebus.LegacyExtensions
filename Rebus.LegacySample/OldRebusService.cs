using Rebus;
using System;
using System.Reflection;

using Topshelf;

namespace Rebus.LegacySample
{
	public class OldRebusService : ServiceControl
	{
		public OldRebusService()
		{
		}

		#region ServiceControl Members
		public bool Start(HostControl hostControl)
		{
			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			return true;
		}

		#endregion
	}
}
