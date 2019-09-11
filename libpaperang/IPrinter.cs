

namespace libpaperang {
	public abstract class BaseTypes {
		public enum Connection {
			None,
			UART,
			USB,
			Bluetooth
		}
		public struct Packet {
			byte Start;
			byte End;
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
			byte[] NoOp;
			byte[] LineFeed;
			byte[] Print;
			byte[] TransmitCrc;
		}
	}
	interface IPrinter {
		short LineWidth { get; }
		BaseTypes.Connection ConnectionMethod { get; }
		BaseTypes.Model PrinterVariant { get; }
		BaseTypes.State Status { get; }
		bool IsPrinterAvailable();
		bool IsPrinterInitialised();
		bool Initialise();
		bool OpenPrinter();
		bool ClosePrinter();
		bool Deinitialise();
		bool WriteBytes(byte[] packet);
		bool WriteBytes(byte[] packet, int delay);
		bool[] ReadBytes();
	}
}
