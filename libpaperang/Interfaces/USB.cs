﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace libpaperang.Interfaces {
	public class USB : IPrinter {
		// constants
		private const ushort iV = 0x4348;
		private const ushort iP = 0x5584;
		public uint MaximumDataSize => 1008;

		// types
		private struct UsbComms {
			public UsbDevice handle;
			public IUsbDevice iface;
			public UsbEndpointReader rx;
			public UsbEndpointWriter tx;
		}
		private UsbComms Printer;

		public short LineWidth {
			get {
				switch(PrinterVariant) {
					case BaseTypes.Model.P1:
						return 48;
					case BaseTypes.Model.P2:
						return 72;
					case BaseTypes.Model.T1:
						throw new PrinterVariantNotSupportedException();
					default:
						throw new InvalidOperationException();
				}
			}
		}
		public short BasePrintDelay {
			get {
				switch(PrinterVariant) {
					//A delay when actually -printing- is necessary; moreso with the P2 model, as higher DPI means more packets for the same physical length of paper
					// and when faced with a print data buffer exhaustion, the printer will simply discard what's in the buffer and start printing whatever data is received after
					case BaseTypes.Model.P1:
						return 100;
					case BaseTypes.Model.P2:
						return 170;
					default:
						throw new InvalidOperationException();
				}
			}
		}
		public BaseTypes.Connection ConnectionMethod => BaseTypes.Connection.USB;
		public BaseTypes.Model PrinterVariant { get; private set; }
		public BaseTypes.State Status => BaseTypes.State.Offline;
		public List<BaseTypes.Printer> AvailablePrinters {
			get {
				List<BaseTypes.Printer> _=new List<BaseTypes.Printer>();
				(from d in UsbDevice.AllDevices
				 where d.Vid == iV &&
					 d.Pid == iP
				 select d).ToList().ForEach(d => _.Add(new BaseTypes.Printer {
					 Id = d.DeviceInterfaceGuids.First(),
					 CommsMethod = BaseTypes.Connection.USB,
					 Address = d.DeviceProperties["Address"].ToString(),
					 Instance = d
				 }));
				return _;
			}
		}
		public bool PrinterAvailable => AvailablePrinters.Count > 0;
		public bool PrinterInitialised { get; private set; } = false;
		public bool PrinterOpen => Printer.handle?.IsOpen ?? false;
		public void OpenPrinter(BaseTypes.Printer printer) {
			bool res;
			try {
				res = ((UsbRegistry)printer.Instance).Open(out Printer.handle);
			} catch(Exception) {
				throw new PrinterNotAvailableException();
			}
			if(!res)
				throw new PrinterNotAvailableException();
		}
		public void ClosePrinter() => Printer.handle?.Close();
		public void Initialise() {
			if(!PrinterOpen)
				throw new PrinterNotInitialisedException();
			//WinUSB-specific
			Printer.iface = Printer.handle as IUsbDevice;
			_ = Printer.iface?.SetConfiguration(1);
			_ = Printer.iface?.ClaimInterface(0);
			Printer.rx = Printer.handle.OpenEndpointReader(ReadEndpointID.Ep01);
			Printer.tx = Printer.handle.OpenEndpointWriter(WriteEndpointID.Ep02);
			PrinterInitialised = true;
		}
		public void Deinitialise() {
			Printer.rx?.Dispose();
			Printer.tx?.Dispose();
			_ = Printer.iface?.ReleaseInterface(0);
			_ = Printer.iface?.Close();
			ClosePrinter();
		}
		public byte[] ReadBytes() {
			byte[] readbuf=new byte[1024];
			_ = Printer.rx.Read(readbuf, 100, out int _);
			return readbuf;
		}
		public bool WriteBytes(byte[] packet) => Printer.tx.Write(packet, 500, out int _) == ErrorCode.None;
		public bool WriteBytes(byte[] packet, int delay) {
			bool _=WriteBytes(packet);
			Thread.Sleep(delay);
			return _;
		}
		public USB(BaseTypes.Model model) => PrinterVariant = model;
	}
}
