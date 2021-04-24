using Rebus.Sagas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.ServerSample
{
	public class TestSagaData : SagaData
	{
		public Guid UserId { get; set; }
		public string UserName { get; set; }
		public DateTime CreationDate { get; set; }
	}
}
