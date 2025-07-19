using System;
namespace ServerPMS.Abstractions.Infrastructure.Logging
{
	public interface IWALLogger
	{
		//properties
		string WALFilePath { get; set; }

		//methods
        void Log(string message);
		void Flush();
		IEnumerable<string> Replay();
	}
}

