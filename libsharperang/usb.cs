using System;
using System.Linq;
using System.Collections.Generic;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Vadavo.NEscPos.Connectors;

namespace libsharperang {
	public class USB : Base {
        //both P1 and P2 models share the same idV and idP over USB
		public readonly ushort idVendor=0x4348;
		public readonly ushort idProduct=0x5584;
        //differentiating between the two is a case of checking the product name
        //P1 is identified as "MiaoMiaoji" or "MTTII Printer"
        //P2 is identified as "Paperang_P2"
        //Notable is that the idV and idP appear to belong not to Paperang, but to a generic CH34x printer adaptor device
        // so it should be a constraint to check that the device descriptors match known details for supported devices,
        // not just idV and idP
		public UsbDevice printer;
		public List<UsbRegistry> pInstances;
		public List<Guid> pIds;
        public IUsb pUsb;

		public USB() {
			ActiveConnectionType=ConnectionType.USB;
		}

		public bool InitUSB() {
			//NOTE - in order for this to work, you must use Zadig to change the driver for
			// the printer from usbprint to WinUSB
            //On MacOS and Linux libusb should "just work" here - testing is pending however.
			if (UsbDevice.AllDevices.Count == 0) return false;
			//genuinely just having some fun with Linq, don't judge me.
			//TODO: work out a way of distinguishing multiple devices with same GUID
			// which happens when two of the same device are connected
			// the only thing that differs seemingly is the Address
			pInstances = (from d in UsbDevice.AllDevices
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
			return (from d in UsbDevice.AllDevices
							where d.DeviceInterfaceGuids.Contains(deviceId)
							select d.DeviceProperties.Where(k => k.Key=="Address").FirstOrDefault().Value).ToList().ConvertAll(v => (int)v);
		}
		public bool OpenUSB() {
			if (UsbDevice.AllDevices.Count == 0) return false;
			if (pIds.Count == 0) return false;
			return OpenUSB(pIds.FirstOrDefault());
		}
		public bool OpenUSB(Guid deviceId) {
			return OpenUSB(deviceId, GetAddressesFromGuid(deviceId).FirstOrDefault());
		}
		public bool OpenUSB(Guid deviceId, int deviceIndex) {
			//first thought is to say "forgive me lord for i have sinned" but i am absolutely not repentant for this
			bool OpenResult = (from d in UsbDevice.AllDevices
												 where d.DeviceInterfaceGuids.Contains(deviceId) && 
														d.DeviceProperties.Where(k=>k.Key=="Address" && (int)k.Value==deviceIndex).Count() > 0
												 select d)
												 .FirstOrDefault()
												 .Open(out UsbDevice handle);
			printer=handle;
			return OpenResult;
		}

        public bool ClaimUSB()
        {
            if (printer is null || printer.IsOpen) return false;
            IUsbDevice p = printer as IUsbDevice;
            p?.SetConfiguration(1);
            p?.ClaimInterface(0);
            pUsb.iPrinter = printer;
            return true;
        }

        

        /*
         * TODO PollPrinter
         * disassembly of Paperang for Mac comes up with the following
         * method.Printer.checkConnect() does:
         * * method.Printer.checkConnectFlag() && return something
         * 0x01a022300
         * 0xffffffff
         * 0x00000000963007772c610eeeba51099919c46d078ff4
         * 
         * need to USBPcap the mac software's comms to confirm above
         * 
         * Paperang for Windows does completely different.
         * ...apparently when the USB URB_CONTROL response says "COMMAND SET:ESC/POS" that means you literally just communicate with it using the ESC/POS protocol
         * ...so i've wasted a lot of time trying to reverse-engineer an open spec..
         */

		public string FoundPrinterGuids() => pIds
																					.ConvertAll(p => p.ToString())
																					.Aggregate((a, b) => a+","+b);
		public string FoundPrinterGuidAddrs(Guid deviceId) => GetAddressesFromGuid(deviceId)
																													.ConvertAll(a => a.ToString())
																													.Aggregate((a, b) => a+","+b);
		public string FoundProdIds() => printer?.Info.ProductString;
	}
}
