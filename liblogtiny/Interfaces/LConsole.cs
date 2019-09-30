using System;

namespace liblogtiny.Interfaces {
	public class LConsole : ILogTiny {
		public void Trace(string msg) => Log(LogTiny.LogLevel.Trace, msg);
		public void Debug(string msg) => Log(LogTiny.LogLevel.Debug, msg);
		public void Verbose(string msg) => Log(LogTiny.LogLevel.Verbose, msg);
		public void Info(string msg) => Log(LogTiny.LogLevel.Info, msg);
		public void Warn(string msg) => Log(LogTiny.LogLevel.Warn, msg);
		public void Error(string msg) => Log(LogTiny.LogLevel.Error, msg);
		public void Critical(string msg) => Log(LogTiny.LogLevel.Critical, msg);
		public void Log(LogTiny.LogLevel level, string msg) {
			if(level >= LogTiny.LogLevel.Warn)
				Console.Error.WriteLine($"{level}: {msg}");
			else
				Raw($"{level}: {msg}");
		}
		public void Raw(string msg) => Console.Out.WriteLine(msg);
	}
}
