using System;

namespace Rebus.Sample.Contracts
{
	public interface IMessageWithUserId
	{
		Guid UserId { get; }
	}
}
