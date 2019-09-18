using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;


/*
 * - 12 bytes is the minimum size of a payload, and 1018 bytes is the maximum
 * - all frames sent to the printer follow the structure of:
 * -- Frame.Begin (1 byte)
 * -- PrinterCmd.* (4 bytes)
 * -- Data (min. 2 bytes? max. 1008 bytes)
 * -- CRC32 sum of the Data block (4 bytes)
 * -- Frame.End (1 byte)
 */

namespace libsharperang {
	public class Frame {
		public DataTransforms transformer=new DataTransforms();

		public enum Opcode {
			SessionBegin = 10,
			Feed,
			PrintBegin = 20,
			PrintContinue,
			CrcTransmit = 30
		}
		private byte FrameStart	= 0x02;
		private byte FrameEnd		= 0x03;
		private byte[] ResolveOpcode(Opcode opcode) {
			switch (opcode) {
				case Opcode.SessionBegin:  return new byte[] { 0x06, 0x00, 0x02, 0x00 };
				case Opcode.Feed:          return new byte[] { 0x1a, 0x00, 0x02, 0x00 };
				case Opcode.CrcTransmit:   return new byte[] { 0x18, 0x01, 0x04, 0x00 };
				default: throw new NullReferenceException();
			}
		}
		public byte[] Build(Opcode opcode, byte[] data) => Build(ResolveOpcode(opcode), data, transformer);
		public byte[] Build(byte[] opcode, byte[] data) => Build(opcode, data, transformer);
		public byte[] Build(Opcode opcode, byte[] data, DataTransforms transformer) => Build(ResolveOpcode(opcode), data, transformer);
		public byte[] Build(byte[] opcode, byte[] data, DataTransforms transformer) {
			byte[] result=new byte[data.Length+10];
			result[0]=FrameStart;
			result[result.Length-1]=FrameEnd;
			Buffer.BlockCopy(opcode, 0, result, 1, 4);
			Buffer.BlockCopy(data, 0, result, 5, data.Length);
			Buffer.BlockCopy(transformer.GetHashSum(data), 0, result, result.Length-5, 4);
			return result;
		}
		public byte[] BuildTransmitCrc() {
			if (!transformer.IsCrcInitialised()) transformer.InitialiseCrc(0x77c40d4d^0x35769521);
			DataTransforms _=new DataTransforms();
			_.InitialiseCrc();
			return Build(Opcode.CrcTransmit, transformer.GetCrcKeyBytes(), _);
		}
	}
	public class USBPrinter : Base, IPrinter {
			// TODO - work out if it's possible to get this working with the default usbprint.inf driver Windows uses on plugging in
			//  otherwise any user would have to first use Zadig to change the driver to WinUSB
		private UsbDevice _uDv;
		private UsbEndpointWriter _uWr;
		private UsbEndpointReader _uRd;
		UsbDevice IPrinter.uDv { get => _uDv; set { } }
		UsbEndpointWriter IPrinter.uWr { get => _uWr; set { } }
		UsbEndpointReader IPrinter.uRd { get => _uRd; set { } }

		private ushort idVendor=0x4348;
		private ushort idProduct=0x5584;
		public Frame builder=new Frame();
		public List<Guid> IDs;
		public List<UsbRegistry> Devices;

		public USBPrinter() {
			ActiveConnectionType = ConnectionType.USB;
			ImageWidth = 72;
		}
		public USBPrinter(int w) {
			ActiveConnectionType = ConnectionType.USB;
			ImageWidth = w;
		}
		~USBPrinter() {
			Close();
			ActiveConnectionType=ConnectionType.None;
		}

		public void Initialise() {
			if (UsbDevice.AllDevices.Count == 0) throw new KeyNotFoundException("No WinUSB or LibUSB devices found");
			Devices=(from d in UsbDevice.AllDevices
							 where d.Vid == idVendor && d.Pid == idProduct
							 select d)
							.ToList<UsbRegistry>();
			if (Devices.Count == 0) throw new KeyNotFoundException("No supported devices found!");
			IDs=(from d in Devices
					 select d.DeviceInterfaceGuids[0])
					.Distinct()
					.ToList<Guid>();
		}

		public List<int> GetAddressesFromGuid(Guid deviceId) => (from d in UsbDevice.AllDevices
																														 where d.DeviceInterfaceGuids.Contains(deviceId)
																														 select d.DeviceProperties
																																		 .Where(k => k.Key=="Address")
																																		 .FirstOrDefault().Value)
																														 .ToList().ConvertAll(v => (int)v);
		public string FoundPrinterGuids() => IDs?
																					.ConvertAll(p => p.ToString())
																					.Aggregate((a, b) => a+","+b);
		public string FoundPrinterGuidAddrs(Guid deviceId) => GetAddressesFromGuid(deviceId)
																													.ConvertAll(a => a.ToString())
																													.Aggregate((a, b) => a+","+b);
		public string FoundProdIds() => _uDv?.Info.ProductString;
		public bool Open() {
			return UsbDevice.AllDevices.Count == 0 ||
					!IsPrinterPresent()
				? false
				: Open(IDs.FirstOrDefault());
		}
		public bool Open(Guid deviceId) => Open(deviceId,
																									GetAddressesFromGuid(deviceId).FirstOrDefault());
		public bool Open(Guid deviceId, int deviceIndex) {
			if (!IsPrinterPresent()) return false;
			//first thought is to say "forgive me lord for i have sinned" but i am absolutely not repentant for this
			bool OpenResult = (from d in UsbDevice.AllDevices
												 where d.DeviceInterfaceGuids.Contains(deviceId) &&
														d.DeviceProperties.Where(k=>k.Key=="Address" && (int)k.Value==deviceIndex).Count() > 0
												 select d)
												 .FirstOrDefault()
												 .Open(out UsbDevice handle);
			_uDv=handle;
			return OpenResult;
		}
		public bool Claim() {
			if (!IsPrinterPresent() ||
					_uDv is null ||
					!_uDv.IsOpen) {
				return false;
			}

			IUsbDevice p = _uDv as IUsbDevice;
			p?.SetConfiguration(1);
			p?.ClaimInterface(0);
			return true;
		}
		public void Close() {
			_uWr?.Dispose();
			_uRd?.Dispose();
			IUsbDevice p=_uDv as IUsbDevice;
			_=p?.ReleaseInterface(0);
			_=p?.Close();
			_uDv?.Close();
		}
		public void TransmitCrcKey() => WriteBytes(builder.BuildTransmitCrc());
		public void StartSession() => StartSession(new byte[2] { 0, 0 });
		public void StartSession(byte[] data) => WriteBytes(builder.Build(Frame.Opcode.SessionBegin, data));
		public void InitPrinter() {
			TransmitCrcKey();
			NoOp(); 
			PollPrinter();
		}
		public void PollPrinter() {
			StartSession();
			NoOp();
		}
		public void Feed() => Feed(100);
		//Feed 0 is equivalent to a no-op, though on the P2 model it appears to engage the roller even though it doesn't ultimately result in any rotation
		public void NoOp() => Feed(0);
		public void Feed(int milliseconds) => WriteBytes(
			builder.Build(Frame.Opcode.Feed, BitConverter.GetBytes(
					transform.SwapEndianness(0x00000000 | (((((
						(uint)milliseconds & 0xFFU) << 16) |
						(uint)milliseconds) & 0xFFFF00U) >> 8))).Skip(2).ToArray()));
		public void PrintBytes(byte[] data, bool autofeed=true) {
			List<byte[]> datas = data
															.Select((b, i) => new {Index = i, Value = b })
															.GroupBy(b => b.Index/1008)
															.Select(b => b.Select(bb => bb.Value).ToArray())
															.ToList();
			datas.ForEach(b => WriteBytes(builder.Build(transform.GeneratePrintOpcode(b), b)));
			if (autofeed) Feed(200);
		}
		public bool IsPrinterPresent() => (Devices != null && Devices.Count > 0);
		bool IPrinter.Initialised() => Initialised();
		byte[] IPrinter.ReadBytes() => ReadBytes();
		bool IPrinter.WriteBytes(byte[] Frame) => WriteBytes(Frame);
		public bool Initialised() => (_uDv!=null);
		public byte[] ReadBytes() {
			if (_uRd==null) _uRd=_uDv?.OpenEndpointReader(ReadEndpointID.Ep01);
			byte[] bRead=new byte[1024];
			_=_uRd.Read(bRead, 100, out int _);
			return bRead;
		}
		public bool WriteBytes(byte[] Frame) {
			return WriteBytes(Frame, ImageWidth*3);
		}
		public bool WriteBytes(byte[] Frame, int milliseconds) {
			if (_uWr==null) _uWr=_uDv?.OpenEndpointWriter(WriteEndpointID.Ep02);
			bool result = _uWr.Write(Frame, 500, out int _) == ErrorCode.None;
			Thread.Sleep(milliseconds);
			return result;
		}
	}
}