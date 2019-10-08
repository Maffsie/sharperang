using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using liblogtiny;
using libpaperang.Helpers;
using libpaperang.Interfaces;

namespace libpaperang.Main {
	public class Paperang {
		public IPrinter Printer;
		public Transforms Transform;
		public CRC Crc;
		public ILogTiny logger;
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
		public void SetLogContext(ILogTiny logger) => this.logger = logger;
		public void Initialise() {
			logger?.Trace($"Initialising libpaperang with {Printer.AvailablePrinters.Count} device(s) connected");
			Printer.OpenPrinter(Printer.AvailablePrinters.First());
			Printer.Initialise();
			logger?.Trace("Initialised, sending handshake");
			Handshake();
		}
		public void Handshake() {
			WriteBytes(
				Transform.Packet(
					BaseTypes.Operations.CrcTransmit,
					Crc.GetCrcIvBytes(),
					new CRC(MagicValue)));
			NoOp();
			Feed(0);
			NoOp();
		}
		public async Task HandshakeAsync() => await Task.Run(() => Handshake());
		public void WriteBytes(byte[] packet) {
			logger?.Trace($"Writing packet with length {packet.Length} to printer");
			_ = Printer.WriteBytes(packet);
		}
		public async Task WriteBytesAsync(byte[] packet) => await Task.Run(() => WriteBytes(packet));
		public void WriteBytes(byte[] packet, int ms) {
			logger?.Trace($"Writing packet with length {packet.Length} to printer with delay of {ms}ms");
			_ = Printer.WriteBytes(packet, ms);
		}
		public async Task WriteBytesAsync(byte[] packet, int ms) => await Task.Run(() => WriteBytes(packet, ms));
		public void Feed(uint ms) {
			logger?.Trace($"Feeding for {ms}ms");
			WriteBytes(
				Transform.Packet(BaseTypes.Operations.LineFeed,
					Transform.Arg(BaseTypes.Operations.LineFeed, ms),
				Crc));
		}
		public async Task FeedAsync(uint ms) => await Task.Run(() => Feed(ms));
		public void NoOp() => WriteBytes(
			Transform.Packet(BaseTypes.Operations.NoOp, new byte[] { 0, 0 }, Crc));
		public async Task NoOpAsync() => await Task.Run(() => NoOp());
		public void Poll() {
			logger?.Trace("Polling attached printer");
			Feed(0);
			NoOp();
		}
		public async Task PollAsync() => await Task.Run(() => Poll());
		//Improvement note - feed delay should scale up dependent on how dark the image is! dark parts take longer to print than light parts. maybe add 1ms of delay for every 50-100 dark pixels??
		public void PrintBytes(byte[] data, bool autofeed = true) {
			logger?.Trace($"PrintBytes() invoked with data length of {data.Length}");
			List<byte[]> segments = data
				.Select((b,i) => new {Index=i,Value=b })
				.GroupBy(b=>b.Index/Printer.MaximumDataSize)
				.Select(b=>b.Select(bb=>bb.Value).ToArray())
				.ToList();
			logger?.Trace($"data split into {segments.Count} segment(s)");
			segments.ForEach(b => WriteBytes(
				Transform.Packet(
					Transform.Arg(BaseTypes.Operations.Print, (uint)b.Length),
					b,
					Crc),
				Printer.BasePrintDelay + (b.Length / Printer.LineWidth)));
			if(autofeed)
				Feed(185);
		}
		public async Task PrintBytesAsync(byte[] data, bool autofeed = true) => await Task.Run(() => PrintBytes(data, autofeed));
	}
}
