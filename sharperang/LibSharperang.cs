#define LIBUSB
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Device.Net;
#if !LIBUSB
using Usb.Net.Windows;
#else
using Device.Net.LibUsb;
#endif

namespace sharperang {
	class LibSharperang {
		private static readonly DebugLogger Logger = new DebugLogger();
		private static readonly DebugTracer Tracer = new DebugTracer();
		private LogBridge log;
		private FilterDeviceDefinition PaperangFilter;
		private List<FilterDeviceDefinition> PaperangFilters;
		public IDevice Printer { get; private set; }
		public DeviceListener PrinterListener { get; }
		public event EventHandler PrinterInit;
		public event EventHandler PrinterDisc;
		public LibSharperang(LogBridge _log) {
			log = _log;
			log.Trace("libsharperang::constructor");
			PaperangFilter = new FilterDeviceDefinition { DeviceType = DeviceType.Usb, VendorId = 0x4348, ProductId = 0x5584 };
			PaperangFilters = new List<FilterDeviceDefinition> { PaperangFilter };
			log.Debug("libsharperang instantiated");
#if LIBUSB
			LibUsbUsbDeviceFactory.Register(Logger, Tracer);
#else
			WindowsUsbDeviceFactory.Register(Logger, Tracer);
#endif
			Logger.LogToConsole = true;
			PrinterListener = new DeviceListener(PaperangFilters, 5000) { Logger = Logger };
			Printer?.Close();
			PrinterListener.DeviceInitialized += PrinterInitEvent;
			PrinterListener.DeviceDisconnected += PrinterDiscEvent;
			PrinterListener.Start();
		}
		~LibSharperang() {
			log.Trace("libsharperang::deconstructor");
			PrinterListener.Dispose();
			Printer?.Close();
			Printer?.Dispose();
			log.Warn("libsharperang destroyed");
		}
		private void PrinterInitEvent(object sender, DeviceEventArgs e) {
			log.Trace("libsharperang::PrinterInitEvent");
			Printer = e.Device;
			PrinterInit?.Invoke(this, new EventArgs());
		}
		private void PrinterDiscEvent(object sender, DeviceEventArgs e) {
			log.Trace("libsharperang::PrinterDiscEvent");
			Printer = null;
			PrinterDisc?.Invoke(this, new EventArgs());
		}
		public async Task InitPrinterAsync() {
			log.Trace("libsharperang::InitPrinterAsync");
			List<IDevice> dev = await DeviceManager.Current.GetDevicesAsync(PaperangFilters);
			Printer = dev.FirstOrDefault();
			if (Printer == null) log.Err("No device found");
			else await Printer.InitializeAsync();
		}
		private async void ListDevs() {
			log.Trace("libsharperang::ListDevs");
			IEnumerable<ConnectedDeviceDefinition> dmdevs = await DeviceManager.Current.GetConnectedDeviceDefinitionsAsync(PaperangFilter);
			log.Debug("List of USB devices via DeviceManager ConnectedDeviceDefinition:");
			foreach (ConnectedDeviceDefinition dev in dmdevs) log.Debug(dev.DeviceId);
		}
	}
}
