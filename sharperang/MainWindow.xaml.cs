using System.Windows;
using libsharperang;

namespace sharperang {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private LogBridge logger;
		//private LibSharperang printer;
		private libsharperang.USB printer;
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
			printer = new libsharperang.USB();
			logger.Debug("libusb init gave "+printer.InitUSB());
			logger.Debug("found Guids are "+printer.FoundPrinterGuids());
			printer.pIds.ForEach(p => logger.Debug("found Addrs for guid "+p.ToString()+" are "+printer.FoundPrinterGuidAddrs(p)));
			logger.Debug("open gave "+printer.OpenUSB(printer.pIds[0]));
		}
	}
}
