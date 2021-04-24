using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Castle;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;

using Rebus;

namespace Rebus.LegacySample.Startup
{
	public class RebusInstaller : IWindsorInstaller
	{

		public RebusInstaller()
		{

		}

		#region IWindsorInstaller Members

		public void Install(IWindsorContainer container, IConfigurationStore store)
		{
			container.Register(Component.For<OldRebusService>().LifestyleSingleton());

			container.AddFacility(new RebusFacility(container));

			container.Register(
				Classes.FromThisAssembly()
				.BasedOn<IHandleMessages>()
				.WithServiceAllInterfaces()
				.LifestyleTransient()
			);
		}

		#endregion
	}
}
