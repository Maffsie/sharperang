﻿using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;


/*
 * the official software seems to transmit a CRC "token" on first detecting the printer (Paperang P1; MiaoMiaoJi):
 * Frame.Begin, PrinterCmd.TransmitCrc, 0x4d0dc477, 0x699295ed, Frame.End
 * This differs when connected to a Paperang P2:
 * Frame.Begin, PrinterCmd.TransmitCrc, 0x18d2a3df, 0x4ec92265, Frame.End
 * 
 * It's not immediately clear to me whether the segment normally containing the CRC is itself a CRC of the token
 *  or if the token is simply 8 bytes rather than 4 and the CRC segment of the frame is omitted for this operation
 * 
 * From what I can tell, calculation of a CRC32 checksum can involve a polynomial and a 'check' value.
 *  It may thus be possible that the Data value is the 'check' value, and the CRC value is the polynomial..
 * In ihciah/miaomiaoji:message_process:crc32(data), the CRC32 value is generated by the zlib.crc32(d,v) function
 *  however I've been unable to reproduce a checksum of 0x906f4576 from a starting value of 0x00 and a polynomial
 *  of any variation of 0x4d0dc477, 0x699295ed
 * Disassembly of the official software indicates it -is- crc32 that's in use, however further investigation is needed
 * Finally, it's not clear whether this value transmitted is required to remain the same at all times
 *  or if the software may use whatever value it wants (whether this is simply a handshake, or whether the specific
 *  value "unlocks" communication with the printer
 * 
 * standard presence checks performed by the official software consist of simply sending two messages every two seconds:
 * Frame.Begin, PrinterCmd.SessionStart, 0x0000, 0x906f4576, Frame.End
 * Frame.Begin, PrinterCmd.SessionEnd, 0x00, 0x906f4576, Frame.End
 * 
 * printing out a blank page yields a 1018-byte payload formed of:
 * Frame.Begin, PrinterCmd.BeginPrint, 0x00*1008, 0x83c0e20f, Frame.End
 * followed by a 634-byte payload formed of:
 * Frame.Begin, PrinterCmd.ContinuePrint, 0x00*624, 0xbc1b6bf4, Frame.End
 * followed by a 12-byte payload formed of:
 * Frame.Begin, PrinterCmd.SessionEnd, 0x2c01, 0xa8347338, Frame.end
 * 
 * printing out a single dash ('-') yields a 1018-byte payload formed of:
 * Frame.Begin, PrinterCmd.BeginPrint, 0x00*858, 0x0fe0, 0x00*46, 0xfe0, 0x00*94, 0xc53ad770, Frame.End
 * followed by a 634-byte payload formed of:
 * Frame.Begin, PrinterCmd.ContinuePrint, 0x00*624, 0xbc1b6bf4, Frame.End
 * followed by a 12-byte payload formed of:
 * Frame.Begin, PrinterCmd.SessionEnd, 0x2c01, 0xa8347338, Frame.end
 * 
 * based on the above we can extrapolate that:
 * - 12 bytes is the minimum size of a payload, and 1018 bytes is the maximum
 * - all frames sent to the printer follow the structure of:
 * -- Frame.Begin (1 byte)
 * -- PrinterCmd.* (4 bytes)
 * -- Data (min. 2 bytes? max. 1008 bytes)
 * -- CRC32 sum of the Data block (4 bytes)
 * -- Frame.End (1 byte)
 * 
 * Also of interest is that, when a model P2 is connected, the printer will actually reply to messages
 * eg., for the standard presence check of Frame.Begin, 0x1a000200, 0x0000, CRC, Frame.End
 * the printer will reply with an almost identical message:
 * Frame.Begin, 0x1a000100, 0x00, CRC, Frame.End
 * The Data segment is a different size (one byte) and the printer appears to be doing its own CRC calculation
 * Finally, the command, 0x1a000100, appears to be an acknowledgement of sorts in response to PrinterCmd.SessionEnd (byte 3 changing from 0x02 to 0x01)
 */

namespace libsharperang {
	public class Frame {
		public DataTransforms transformer=new DataTransforms();

		public enum Opcode {
			SessionBegin = 10,
			SessionEnd,
			PrintBegin = 20,
			PrintContinue,
			CrcTransmit = 30
		}
		private byte FrameStart	= 0x02;
		private byte FrameEnd		= 0x03;
		private byte[] ResolveOpcode(Opcode opcode) {
			switch (opcode) {
				case Opcode.SessionBegin:  return new byte[] { 0x06, 0x00, 0x02, 0x00 };
				case Opcode.SessionEnd:    return new byte[] { 0x1a, 0x00, 0x02, 0x00 };
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
			ActiveConnectionType=ConnectionType.USB;
			ImageWidth=48;
			Initialise();
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
		public void EndSession() => EndSession(new byte[2] { 0, 0 });
		public void EndSession(byte[] data) => WriteBytes(builder.Build(Frame.Opcode.SessionEnd, data));
		public void InitPrinter() {
			TransmitCrcKey();
			EndSession();
			PollPrinter();
		}
		public void PollPrinter() {
			StartSession();
			EndSession();
		}
		public void Feed() => EndSession(new byte[2] { 0x64, 0x00 });
		public void Feed(int milliseconds) => EndSession(BitConverter.GetBytes(transform.SwapEndianness(
			0x00000000 | (((((
				(uint)milliseconds & 0xFFU) << 16) |
				(uint)milliseconds) & 0xFFFF00U) >> 8))).Skip(2).ToArray());
		public void PrintBytes(byte[] data, bool autofeed=true) {
			List<byte[]> datas = data
															.Select((b, i) => new {Index = i, Value = b })
															.GroupBy(b => b.Index/1008)
															.Select(b => b.Select(bb => bb.Value).ToArray())
															.ToList();
			datas.ForEach(b => WriteBytes(builder.Build(transform.GeneratePrintOpcode(b), b)));
			if (autofeed) Feed(60);
		}
		public void PrintTestA(byte[] data) => WriteBytes(builder.Build(Frame.Opcode.PrintBegin, data));
		public void PrintTestB(byte[] data) => WriteBytes(builder.Build(Frame.Opcode.PrintContinue, data));
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
			if (_uWr==null) _uWr=_uDv?.OpenEndpointWriter(WriteEndpointID.Ep02);
			return (_uWr.Write(Frame, 1000, out int _) == ErrorCode.None);
		}
	}
}