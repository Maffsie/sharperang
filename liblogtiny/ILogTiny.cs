namespace liblogtiny {
	public partial class LogTiny {
		public enum LogLevel {
			Trace,
			Debug,
			Verbose,
			Info,
			Warn,
			Error,
			Critical
		};
	}
	public interface ILogTiny {
		void Trace(string msg);
		void Debug(string msg);
		void Verbose(string msg);
		void Info(string msg);
		void Warn(string msg);
		void Error(string msg);
		void Critical(string msg);
		void Log(LogTiny.LogLevel level, string msg);
		void Raw(string msg);
	}
}
