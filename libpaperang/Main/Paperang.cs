using libpaperang;
using libpaperang.Helpers;
using libpaperang.Interfaces;

namespace libpaperang.Main {
	class Paperang {
		public IPrinter Printer;
		public Transforms Transform;
		public CRC Crc;
		private const uint MagicValue = 0x35769521u;

		public Paperang(BaseTypes.Connection connection, BaseTypes.Model model) {
			switch(connection) {
				case BaseTypes.Connection.USB:
					Printer = new USB(model);
					Transform = new Transforms(new BaseTypes.Packet {
						Start = 0x02,
						End = 0x03
					}, new BaseTypes.Opcodes {
						NoOp = new byte[] { 0x06, 0x00, 0x02, 0x00 },
						Print = new byte[] { 0x00, 0x01, 0x00, 0x00 },
						LineFeed = new byte[] { 0x1a, 0x00, 0x02, 0x00 },
						TransmitCrc = new byte[] { 0x18, 0x01, 0x04, 0x00 }
					}, 4);
					Crc = new CRC(0x77c40d4d ^ MagicValue);
					break;
				default:
					throw new PrinterConnectionNotSupportedException();
			}
		}

	}
}
