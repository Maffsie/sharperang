using System;

namespace libpaperang {
	public class USB : IPrinter {
		public short LineWidth => throw new NotImplementedException();

		public BaseTypes.Connection ConnectionMethod => throw new NotImplementedException();

		public BaseTypes.Model PrinterVariant => throw new NotImplementedException();

		public BaseTypes.State Status => throw new NotImplementedException();

		public bool ClosePrinter() => throw new NotImplementedException();
		public bool Deinitialise() => throw new NotImplementedException();
		public bool Initialise() => throw new NotImplementedException();
		public bool IsPrinterAvailable() => throw new NotImplementedException();
		public bool IsPrinterInitialised() => throw new NotImplementedException();
		public bool OpenPrinter() => throw new NotImplementedException();
		public bool[] ReadBytes() => throw new NotImplementedException();
		public bool WriteBytes(byte[] packet) => throw new NotImplementedException();
		public bool WriteBytes(byte[] packet, int delay) => throw new NotImplementedException();
	}
}
