using System.ComponentModel;

namespace sharperang {
	class LogBridge : INotifyPropertyChanged {
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
				else _LogBuffer += value + "\n";
				OnPropertyChanged("LogBuffer");
			}
		}

		public void ClearBuffer() => LogBuffer="!clearlog";
		public void Trace(string line) => LogBuffer="TRCE: "+line;
		public void Debug(string line) => LogBuffer="DEBG: "+line;
		public void Verbose(string line) => LogBuffer="VERB: "+line;
		public void Info(string line) => LogBuffer="INFO: "+line;
		public void Warn(string line) => LogBuffer="WARN: "+line;
		public void Err(string line) => LogBuffer="ERR!: "+line;
		public void Critical(string line) => LogBuffer="CRIT: "+line;
	}
}
