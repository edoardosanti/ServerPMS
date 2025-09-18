
using System;
namespace ServerPMS.Abstractions.Infrastructure.ClientCommunication
{
	public interface IStreamParser
	{
		public Task<byte[]> GetMessageBytesAsync();
	}
}

