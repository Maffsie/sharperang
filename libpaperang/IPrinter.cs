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
			None,
			P1, // Original model; 57mm feed, 48-byte lines (200DPI), LiPo battery (1Ah)
			P1S,// Original "special edition" model; identical to P1 but in different colours
			T1, // Label printer model; 15mm feed, unknown-byte lines, 4xAAA battery
			P2, // Hi-DPI model; 57mm feed, 96-byte lines (300DPI), LiPo battery (1Ah)
			P2S // Hi-DPI "special edition" model; identical to P2 but includes Pomodoro timer functionality
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
		//I'm sure there are other opcodes available but these are the "core" ones implemented by the official MiaoMiaoJi chinese Windows and OSX apps
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
	public interface IPrinter {
		short LineWidth { get; }
		short BasePrintDelay { get; }
		uint MaximumDataSize { get; }
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
