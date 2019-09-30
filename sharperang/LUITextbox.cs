using liblogtiny;
using System.ComponentModel;

namespace sharperang {
	class LUITextbox : ILogTiny, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		private string _LogBuffer = "";
		public string LogBuffer {
			//get { return _LogBuffer; }
			get => _LogBuffer;
			set {
				if (value == _LogBuffer) return;
				if (value == "!clearlog") _LogBuffer = "";
				else _LogBuffer = value + "\n" + _LogBuffer;
				OnPropertyChanged("LogBuffer");
			}
		}
		public void ClearBuffer() => LogBuffer="!clearlog";
		public void Trace(string msg) => Log(LogTiny.LogLevel.Trace, msg);
		public void Debug(string msg) => Log(LogTiny.LogLevel.Debug, msg);
		public void Verbose(string msg) => Log(LogTiny.LogLevel.Verbose, msg);
		public void Info(string msg) => Log(LogTiny.LogLevel.Info, msg);
		public void Warn(string msg) => Log(LogTiny.LogLevel.Warn, msg);
		public void Error(string msg) => Log(LogTiny.LogLevel.Error, msg);
		public void Critical(string msg) => Log(LogTiny.LogLevel.Critical, msg);
		public void Log(LogTiny.LogLevel level, string msg) => LogBuffer=$"{level}: {msg}";
		public void Raw(string msg) => LogBuffer=msg;
	}
}
