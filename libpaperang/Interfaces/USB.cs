using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace libpaperang.Interfaces {
	public class USB : IPrinter {
		// constants
		private const ushort iV = 0x4348;
		private const ushort iP = 0x5584;
		private const short MaxDataSize = 1008;

		// types
		private struct UsbComms {
			public Guid id;
			public UsbDevice handle;
			public IUsbDevice iface;
			public UsbEndpointReader rx;
			public UsbEndpointWriter tx;
		}
		private UsbComms Printer;
		private BaseTypes.Model iModel;
		public short LineWidth {
			get {
				switch (iModel) {
					case BaseTypes.Model.P1: return 48;
					case BaseTypes.Model.P2: return 72;
					case BaseTypes.Model.P2S: return 72; //assumption
					case BaseTypes.Model.T1: throw new PrinterVariantNotSupportedException();
					default: throw new InvalidOperationException();
				}
			}
		}

		public BaseTypes.Connection ConnectionMethod => BaseTypes.Connection.USB;
		public BaseTypes.Model PrinterVariant => iModel;
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

		public bool PrinterInitialised => false;

		public bool PrinterOpen => false;

		public void ClosePrinter() {
			Printer.rx?.Dispose();
			Printer.tx?.Dispose();
			_=Printer.iface?.ReleaseInterface(0);
			_=Printer.iface?.Close();
			_=Printer.handle?.Close();
		}
		public void Deinitialise() => throw new NotImplementedException();
		public void Initialise() {
			if (!PrinterOpen || !Printer.handle.IsOpen) throw new PrinterNotInitialisedException();
			//WinUSB-specific
			Printer.iface=Printer.handle as IUsbDevice;
			Printer.iface?.SetConfiguration(1);
			Printer.iface?.ClaimInterface(0);
			Printer.rx=Printer.handle.OpenEndpointReader(ReadEndpointID.Ep01);
			Printer.tx=Printer.handle.OpenEndpointWriter(WriteEndpointID.Ep02);

		}
		public void OpenPrinter(BaseTypes.Printer printer) {
			bool res;
			try {
				res=((UsbRegistry)printer.Instance).Open(out Printer.handle);
			} catch (Exception) {
				throw new PrinterNotAvailableException();
			}
			if (!res) throw new PrinterNotAvailableException();
			}
		}
		public bool[] ReadBytes() => throw new NotImplementedException();
		public void WriteBytes(byte[] packet) => throw new NotImplementedException();
		public void WriteBytes(byte[] packet, int delay) => throw new NotImplementedException();

		public USB(BaseTypes.Model model) => iModel = model;
	}
}
