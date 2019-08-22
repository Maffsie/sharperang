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
			//TODO: work out a way of distinguishing multiple devices with same GUID
			// which happens when two of the same device are connected
			// the only thing that differs seemingly is the Address
			pInstances = (from d in UsbDevice.AllWinUsbDevices
										where d.Vid == idVendor && d.Pid == idProduct
										select d)
										.ToList<UsbRegistry>();
			pIds = (from d in pInstances
							select d.DeviceInterfaceGuids[0])
							.Distinct()
							.ToList<Guid>();
			return (pIds.Count > 0);
		}
		public List<int> GetAddressesFromGuid(Guid deviceId) {
			return (from d in UsbDevice.AllWinUsbDevices
							where d.DeviceInterfaceGuids.Contains(deviceId)
							select d.DeviceProperties.Where(k => k.Key=="Address").FirstOrDefault().Value).ToList().ConvertAll(v => (int)v);
		}
		public bool OpenUSB() {
			if (UsbDevice.AllWinUsbDevices.Count == 0) return false;
			if (pIds.Count == 0) return false;
			return OpenUSB(pIds.FirstOrDefault());
		}
		public bool OpenUSB(Guid deviceId) {
			return OpenUSB(deviceId, GetAddressesFromGuid(deviceId).FirstOrDefault());
		}
		public bool OpenUSB(Guid deviceId, int deviceIndex) {
			//first thought is to say "forgive me lord for i have sinned" but i am absolutely not repentant for this
			bool OpenResult = (from d in UsbDevice.AllWinUsbDevices
												 where d.DeviceInterfaceGuids.Contains(deviceId) && 
														d.DeviceProperties.Where(k=>k.Key=="Address" && (int)k.Value==deviceIndex).Count() > 0
												 select d)
												 .FirstOrDefault()
												 .Open(out UsbDevice handle);
			printer=handle;
			return OpenResult;
		}

		public string FoundPrinterGuids() => pIds
																					.ConvertAll(p => p.ToString())
																					.Aggregate((a, b) => a+","+b);
		public string FoundPrinterGuidAddrs(Guid deviceId) => GetAddressesFromGuid(deviceId)
																													.ConvertAll(a => a.ToString())
																													.Aggregate((a, b) => a+","+b);
		public string FoundProdIds() => printer?.Info.ProductString;
	}
}
