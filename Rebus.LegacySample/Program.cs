using System;

using Castle.Windsor;

using Topshelf;

namespace Rebus.LegacySample
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var container = new WindsorContainer())
			{
				container.Install(
					new Startup.RebusInstaller()
				);

				var service = container.Resolve<OldRebusService>();
				HostFactory.Run(x =>
				{
					x.SetServiceName("RebusTestOld");
					x.SetDescription("Old Rebus Test");
					x.StartAutomatically();
					x.RunAsLocalSystem();
					x.Service(() => service, s =>
					{
						s.AfterStartingService(ctx =>
						{
							Console.WriteLine("Initialized");
						});
					});
				});
			}
		}
	}
}
