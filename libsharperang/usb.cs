using System;
using System.Linq;
using System.Collections.Generic;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace libsharperang {
	public class USB : Base {
		public readonly ushort idVendor=0x4348;
		public readonly ushort idProduct=0x5584;
		public UsbDevice printer;
		public List<UsbRegistry> pInstances;
		public List<Guid> pIds;

		public USB() {
			ActiveConnectionType=ConnectionType.USB;
		}

		public bool InitUSB() {
			//NOTE - in order for this to work, you must use Zadig to change the driver for
			// the printer from usbprint to WinUSB
			if (UsbDevice.AllWinUsbDevices.Count == 0) return false;
			//genuinely just having some fun with Linq, don't judge me.
			pInstances = (from d in UsbDevice.AllWinUsbDevices
										where d.Vid == idVendor && d.Pid == idProduct
										select d)
										.ToList<UsbRegistry>();
			pIds = (from d in pInstances
							select d.DeviceInterfaceGuids[0])
							//.Distinct()
							.ToList<Guid>();
			return (pIds.Count > 0);
		}
		public bool OpenUSB() {
			if (UsbDevice.AllWinUsbDevices.Count == 0) return false;
			if (pIds.Count == 0) return false;
			return OpenUSB(pIds.FirstOrDefault());
		}
		public bool OpenUSB(Guid deviceId) {
			bool OpenResult = UsbDevice.OpenUsbDevice(
				ref deviceId, out UsbDevice handle);
			printer=handle;
			return OpenResult;
		}

		public string FoundPrinterGuids() => pIds
																					.ConvertAll<string>(p => p.ToString())
																					.Aggregate((a, b) => a+","+b);
		public string FoundProdIds() => printer?.Info.ProductString;
	}
}
