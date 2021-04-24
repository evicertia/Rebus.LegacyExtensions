using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.ClientSample
{
	public interface ISendTestService
	{
		public Task<bool> SendMessage();
	}
}
