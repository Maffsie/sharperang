using System;
using System.Collections.Generic;
using System.Text;
using Vadavo.NEscPos;
using Vadavo.NEscPos.Printable;

namespace libsharperang {
	public abstract class Base {
		public enum ConnectionType {
			None,
			UART,
			USB,
			Bluetooth
		}
		public string Model { get; internal set; }
		public string FirmwareVer { get; internal set; }
		public int Battery { get; internal set; }
		public ConnectionType ActiveConnectionType { get; internal set; } = ConnectionType.None;
	}
}
