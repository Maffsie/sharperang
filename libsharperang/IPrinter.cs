using LibUsbDotNet;

namespace libsharperang {
	interface IPrinter {
		UsbDevice uDv { get; set; }
		UsbEndpointWriter uWr { get; set; }
		UsbEndpointReader uRd { get; set; }
		bool Initialised();
		bool WriteBytes(byte[]Frame);
		byte[] ReadBytes();
	}
}
