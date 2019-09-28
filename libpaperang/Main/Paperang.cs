using libpaperang;
using libpaperang.Helpers;
using libpaperang.Interfaces;
using System.Linq;
using System.Collections.Generic;

namespace libpaperang.Main {
	public class Paperang {
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
		public void Initialise() {
			Printer.OpenPrinter(Printer.AvailablePrinters.First());
			Printer.Initialise();
			Handshake();
		}
		public void Handshake() {
			_ = Printer.WriteBytes(
				Transform.Packet(
					BaseTypes.Operations.CrcTransmit,
					Crc.GetCrcIvBytes(),
					new CRC(MagicValue)));
			NoOp();
			Feed(0);
			NoOp();
		}
		public void Feed(uint ms) => Printer.WriteBytes(
			Transform.Packet(BaseTypes.Operations.LineFeed,
				Transform.Arg(BaseTypes.Operations.LineFeed, ms),
				Crc));
		public void NoOp() => Printer.WriteBytes(
			Transform.Packet(BaseTypes.Operations.NoOp, new byte[] { 0, 0 }, Crc));
		public void PrintBytes(byte[] data, bool autofeed = true) {
			List<byte[]> segments = data
				.Select((b,i) => new {Index=i,Value=b })
				.GroupBy(b=>b.Index/1008)
				.Select(b=>b.Select(bb=>bb.Value).ToArray())
				.ToList();
			segments.ForEach(b => Printer.WriteBytes(
				Transform.Packet(
					Transform.Arg(BaseTypes.Operations.Print, (uint)b.Length),
					b,
					Crc),
				200-(b.Length/Printer.LineWidth)));
		}
	}
}
