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
			logger.Debug("Open => "+printer?.Open());
			logger.Debug("Claim => " + printer?.Claim());
			logger.Debug("Init => "+BitConverter.ToString(printer.builder.BuildTransmitCrc()).Replace('-', ' '));
			printer.InitPrinter();
			logger.Debug("Printer initialised and ready");
		}
		private void BtTestLine_Click(object sender, RoutedEventArgs e) {
			byte[] data = new byte[384];
			//having spent some time examining this, each bit in a given byte corresponds to a dot on the thermal impression plate
			//and each set of 192 bytes constitutes a single line
			//giving a size of 3072 distinct dots per line
			data[5] = 0x10; data[4]=0x10; data[3]=0x20; data[2]=0x20; data[1]=0x30; data[0]=0x30;
			data[192] = 0x10; data[193]=0x10;data[194]=0x20;data[195]=0x20;data[196]=0x30;data[197]=0x30;
			printer.PrintBytes(data);
		}
	}
}
