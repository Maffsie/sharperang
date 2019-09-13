using System;
using System.Collections.Generic;

namespace libpaperang {
	public abstract class BaseTypes {
		public enum Connection {
			None,
			UART,
			USB,
			Bluetooth
		}
		public struct Packet {
			public byte Start;
			public byte End;
		}
		public enum Model {
			P1,
			T1,
			P2,
			P2S
		}
		public enum State {
			Offline,
			Available,
			Ready,
			NotReady,
			Printing,
		}
		public enum Fault {
			PaperEmpty,
			DoorOpen
		}
		public enum Operations {
			NoOp,
			LineFeed,
			CrcTransmit,
			Print
		}
		public struct Opcodes {
			public byte[] NoOp;
			public byte[] LineFeed;
			public byte[] Print;
			public byte[] TransmitCrc;
		}
		public struct Printer {
			public Connection CommsMethod;
			public Model Variant;
			public Guid Id;
			public string Address;
			public dynamic Instance;
		}
	}
	interface IPrinter {
		short LineWidth { get; }
		BaseTypes.Connection ConnectionMethod { get; }
		BaseTypes.Model PrinterVariant { get; }
		BaseTypes.State Status { get; }
		bool PrinterAvailable { get; }
		bool PrinterInitialised { get; }
		bool PrinterOpen { get; }
		List<BaseTypes.Printer> AvailablePrinters { get; }
		void Initialise();
		void OpenPrinter(BaseTypes.Printer printer);
		void ClosePrinter();
		void Deinitialise();
		bool WriteBytes(byte[] packet);
		bool WriteBytes(byte[] packet, int delay);
		byte[] ReadBytes();
	}
}
