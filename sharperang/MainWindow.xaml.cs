using libsharperang;
using System;
using System.Windows;

namespace sharperang {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private LogBridge logger;
		private USBPrinter printer=new USBPrinter();
		public MainWindow() {
			InitializeComponent();
			logger = new LogBridge();
			gMain.DataContext = logger;
			logger.Info("Application started");
		}
		private void BtClearLog_Click(object sender, RoutedEventArgs e) =>
			logger.ClearBuffer();
		private void BtInitUSB_Click(object sender, RoutedEventArgs e) {
			logger.Info("USB Initialising");
			//printer = new LibSharperang(logger);
			logger.Debug("IsPrinterPresent => "+printer.IsPrinterPresent());
			logger.Debug("FoundPrinterGuids => "+printer.FoundPrinterGuids());
			printer?.IDs?.ForEach(p => logger.Debug("FoundPrinterGuidAddrs "+p.ToString()+" => "+printer?.FoundPrinterGuidAddrs(p)));
			logger.Debug("OpenUSB => "+printer?.Open());
			logger.Debug("ClaimUSB => " + printer?.Claim());
			//logger.Debug("IUsb::Initialised => " + printer?.pUsb?.Initialised());
		}
		private void BtTestLine_Click(object sender, RoutedEventArgs e) {
			logger.Debug("printer::TransmitCrc() => "+BitConverter.ToString(printer.builder.BuildTransmitCrc()).Replace('-', ' '));
			logger.Debug("printer::TestCRC(SessionBegin, 0x0000) => "+BitConverter.ToString(printer.builder.Build(Frame.Opcode.SessionBegin, new byte[] { 0x00, 0x00 })).Replace('-',' '));
			logger.Debug("Transmit CRC and session preamble to printer, if validation needed please follow in wireshark");
			printer.TransmitCrcKey();
			printer.EndSession();
			printer.StartSession();
			printer.EndSession();
		}
	}
}
