using libsharperang;
using System;
using System.Windows;

//testing
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace sharperang {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private LogBridge logger;
		private USBPrinter printer=new USBPrinter();
		private Bitmap bimg;

		public static Bitmap ConvertTo1Bit(Bitmap input) {
			byte[] masks = new byte[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };
			Bitmap output = new Bitmap(input.Width, input.Height, PixelFormat.Format1bppIndexed);
			sbyte[,] data = new sbyte[input.Width, input.Height];
			BitmapData inputData = input.LockBits(new Rectangle(0, 0, input.Width, input.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			try {
				IntPtr scanLine = inputData.Scan0;
				byte[] line = new byte[inputData.Stride];
				for (int y = 0; y < inputData.Height; y++, scanLine += inputData.Stride) {
					Marshal.Copy(scanLine, line, 0, line.Length);
					for (int x = 0; x < input.Width; x++) {
						data[x, y] = (sbyte)(64 * (GetGreyLevel(line[(x * 3) + 2], line[(x * 3) + 1], line[(x * 3) + 0]) - 0.5));
					}
				}
			} finally {
				input.UnlockBits(inputData);
			}
			BitmapData outputData = output.LockBits(new Rectangle(0, 0, output.Width, output.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
			try {
				IntPtr scanLine = outputData.Scan0;
				for (int y = 0; y < outputData.Height; y++, scanLine += outputData.Stride) {
					byte[] line = new byte[outputData.Stride];
					for (int x = 0; x < input.Width; x++) {
						bool j = data[x, y] > 0;
						if (j) line[x / 8] |= masks[x % 8];
						sbyte error = (sbyte)(data[x, y] - (j ? 32 : -32));
						if (x < input.Width - 1) data[x + 1, y] += (sbyte)(7 * error / 16);
						if (y < input.Height - 1) {
							if (x > 0) data[x - 1, y + 1] += (sbyte)(3 * error / 16);
							data[x, y + 1] += (sbyte)(5 * error / 16);
							if (x < input.Width - 1) data[x + 1, y + 1] += (sbyte)(1 * error / 16);
						}
					}
					Marshal.Copy(line, 0, scanLine, outputData.Stride);
				}
			} finally {
				output.UnlockBits(outputData);
			}
			return output;
		}

		public static double GetGreyLevel(byte r, byte g, byte b) => ((r * 0.299) + (g * 0.587) + (b * 0.114)) / 255;

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
			bimg=new Bitmap(bimg, 384, 543);
			Bitmap fimg=new Bitmap(768, 543);
			for(int h=0;h<bimg.Height;h++) {
				for(int w=0;w<384;w++) {
					fimg.SetPixel(w, h, bimg.GetPixel(w, h));
				}
			}
			Bitmap tmp = ConvertTo1Bit(fimg);
			byte[] img;
			
			printer.PrintBytes(img, false);
		}
	}
}
