using libsharperang;
using System;
using System.Windows;

//testing
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace sharperang {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private LogBridge logger;
		private USBPrinter printer=new USBPrinter();
		private Bitmap bimg;
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
			byte[] data = new byte[72];
			byte[] data2 = new byte[95];
			//for each byte (0000 0000) every 4 bits is a "dot" with variable width.
			//having spent some time examining this, each bit in a given byte corresponds to a dot on the thermal impression plate
			// every 96 bytes is one line, and the first 48 bytes of each line is the actual dot data. it's unclear to me what the other half is for. P2 support maybe?
			//giving a size of 768 distinct dots per line
			//line length of P1 is 48 bytes
			//line length of P2 is 72 bytes
			//unsure why each line is stored in a buffer of 96 bytes, but okay.
			//Paperang P1 prints bytes on one line from byte 0 to byte 47
			//Paperang P2 prints bytes on one line from byte 0 to byte 71
			data[0] = 0x55; data[8] = 0x77; data[16] = 0x77; data[24] = 0x77; data[32] = 0x77; data[40] = 0x77;

			data[48] = 0x77; data[56] = 0x77; data[64] = 0x77; data[71] = 0x77; //data[72] = 0x77; data[80] = 0x77; data[88] = 0x77;
			//data[5] = 0x10; data[4]=0x10; data[3]=0x20; data[2]=0x20; data[1]=0x30; data[0]=0x30;
			/*data2[0] = 0x10; data2[1]=0x10;data2[2]=0x20;data2[3]=0x20;data2[4]=0x30;data2[5]=0x30;
			data2[32] = 0xaa; data[32] = 0x55;
			data2[50] = 0x55; data[50] = 0xaa;
			data2[87] = 0xaa; data[87] = 0x55;*/
			printer.PrintBytes(data, false);
			printer.PrintBytes(data2, false);
		}

		private void BtLoadImage_Click(object sender, RoutedEventArgs e) {
			bimg=new Bitmap("C:/Users/maff/Downloads/Sqrl - Kid Maff (Vectorised).png");
			int w=bimg.Width; int h=bimg.Height;float rt=(float)w/h;
			h=(int)(384*rt);
			bimg=new Bitmap(bimg, 384, h);
			Bitmap tmp = new Bitmap(bimg.Width, bimg.Height, PixelFormat.Format1bppIndexed);
			int width = bimg.Width;
			int height = bimg.Height;
			Color p;
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					p = bimg.GetPixel(x, y);
					int a = p.A;
					int r = p.R;
					int g = p.G;
					int b = p.B;
					int avg = (int)(0.2989*r + 0.5870*g + 0.1140*b)/3;
					avg = avg < 128 ? 0 : 255;
					bimg.SetPixel(x, y, Color.FromArgb(a, avg, avg, avg));
				}
			}
			byte[] img;
			using (MemoryStream s = new MemoryStream()) {
				tmp.Save(s, System.Drawing.Imaging.ImageFormat.Bmp);
				img=s.ToArray();
			}
			printer.PrintBytes(img, false);
		}
	}
}
