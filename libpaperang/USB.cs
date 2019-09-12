using System;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace libpaperang {
    public class USB : IPrinter {
        // constants
        private const ushort iV = 0x4348;
        private const ushort iP = 0x5584;
        private const short MaxDataSize = 1008;

        // types
        private struct UsbComms {
            Guid id;
            UsbDevice handle;
            UsbEndpointReader rx;
            UsbEndpointWriter tx;
        }
        private UsbComms Printer;
        private BaseTypes.Model iModel;
        public short LineWidth { get {
                switch (iModel) {
                    case BaseTypes.Model.P1: return 48;
                    case BaseTypes.Model.P2: return 72;
                    case BaseTypes.Model.P2S: return 72; //assumption
                    case BaseTypes.Model.T1: throw new PrinterVariantNotSupportedException();
                    default: throw new InvalidOperationException();
                }
            } }

        public BaseTypes.Connection ConnectionMethod { get => BaseTypes.Connection.USB; }
        public BaseTypes.Model PrinterVariant { get => iModel; }

        public BaseTypes.State Status => throw new NotImplementedException();

        public bool ClosePrinter() => throw new NotImplementedException();
        public bool Deinitialise() => throw new NotImplementedException();
        public bool Initialise() {
            if (!IsPrinterAvailable()) return false;
            return true;
        }
		public bool IsPrinterAvailable() => throw new NotImplementedException();
		public bool IsPrinterInitialised() => throw new NotImplementedException();
		public bool OpenPrinter() => throw new NotImplementedException();
		public bool[] ReadBytes() => throw new NotImplementedException();
		public bool WriteBytes(byte[] packet) => throw new NotImplementedException();
		public bool WriteBytes(byte[] packet, int delay) => throw new NotImplementedException();

        public USB(BaseTypes.Model model) => iModel = model;
	}
}
