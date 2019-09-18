using libsharperang;
using System;
using System.Windows;
using System.Windows.Forms;
//testing
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace sharperang {
	public partial class MainWindow : Window {
		private LogBridge logger;
		private USBPrinter printer=new USBPrinter(48);
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
			printer.Initialise();
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
		private void BtTestLine_Click(object sender, RoutedEventArgs e) => printer.Feed(200);
		private void BtLoadImage_Click(object sender, RoutedEventArgs e) {
			logger.Debug("Loading image for print");
			OpenFileDialog r = new OpenFileDialog {
				Title="Select 1 (one) image file",
				Multiselect=false,
				Filter="PNG files (*.png)|*.png|JPEG files (*.jpe?g)|*.jpg *.jpeg|Jraphics Interchange Format files (*.gif)|*.gif|Bitte-Mappe files (*.bmp)|*.bmp|All of the above|*.jpg *.jpeg *.png *.gif *.bmp",
				AutoUpgradeEnabled=true
			};
			if (r.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) {
				logger.Debug("Image load cancelled");
				return;
			}
			Image _=Image.FromFile(r.FileName);
			logger.Debug("Loaded image " + r.FileName);
			r.Dispose();
			logger.Debug("Disposed of dialog");
			bimg=new Bitmap(_,
											(printer.ImageWidth*8), (int)((double)(printer.ImageWidth*8)*(double)((double)_.Height/(double)_.Width)));
			logger.Debug("Loaded image as Bitmap");
			_.Dispose();
			logger.Debug("Disposed of Image");
			bimg=CopyToBpp(bimg);
			logger.Debug("Converted Bitmap to Bitmap with 1-bit colour depth");
			//BitArray img = new BitArray(bimg.Height*96*8);
			byte[] iimg = new byte[bimg.Height*printer.ImageWidth];
			byte[] img;
			using (MemoryStream s = new MemoryStream()) {
				bimg.Save(s, ImageFormat.Bmp);
				img=s.ToArray();
			}
			logger.Debug("Got bitmap's bytes");
			int startoffset=img.Length-(bimg.Height*printer.ImageWidth);
			logger.Debug("Processing bytes with offset " + startoffset);
			for(int h=0;h<bimg.Height;h++) {
				for (int w=0;w<printer.ImageWidth;w++) {
					iimg[(printer.ImageWidth*(bimg.Height-1-h))+(printer.ImageWidth-1-w)]=(byte)~
						(img[startoffset+(printer.ImageWidth*h)+(printer.ImageWidth-1-w)]);
				}
			}
			logger.Debug("Have print data of length " + iimg.Length);
			bimg.Dispose();
			logger.Debug("Disposed of Bitmap");
			printer.PrintBytes(iimg, false);
			logger.Debug("Feeding for 200ms");
			printer.Feed(200);
		}
		static uint BitSwap1(uint x) => ((x & 0x55555555u) << 1) | ((x & (~0x55555555u)) >> 1);
		static uint BitSwap2(uint x) => ((x & 0x33333333u) << 2) | ((x & (~0x33333333u)) >> 2);
		static uint BitSwap4(uint x) => ((x & 0x0f0f0f0fu) << 4) | ((x & (~0x0f0f0f0fu)) >> 4);
		static uint BitSwap(uint x) => BitSwap4(BitSwap2(BitSwap1(x)));
		static Bitmap CopyToBpp(Bitmap b) {
			int w=b.Width, h=b.Height;
			IntPtr hbm = b.GetHbitmap();
			BITMAPINFO bmi = new BITMAPINFO {
				biSize=40,
				biWidth=w,
				biHeight=h,
				biPlanes=1,
				biBitCount=1,
				biCompression=BI_RGB,
				biSizeImage = (uint)(((w+7)&0xFFFFFFF8)*h/8),
				biXPelsPerMeter=1000000,
				biYPelsPerMeter=1000000
			};
			bmi.biClrUsed=2;
			bmi.biClrImportant=2;
			bmi.cols=new uint[256];
			bmi.cols[0]=MAKERGB(0, 0, 0);
			bmi.cols[1]=MAKERGB(255, 255, 255);
			IntPtr hbm0 = CreateDIBSection(IntPtr.Zero,ref bmi,DIB_RGB_COLORS,out IntPtr _,IntPtr.Zero,0);
			IntPtr sdc = GetDC(IntPtr.Zero);
			IntPtr hdc = CreateCompatibleDC(sdc); SelectObject(hdc, hbm);
			IntPtr hdc0 = CreateCompatibleDC(sdc); SelectObject(hdc0, hbm0);
			BitBlt(hdc0, 0, 0, w, h, hdc, 0, 0, SRCCOPY);
			Bitmap b0 = Image.FromHbitmap(hbm0);
			DeleteDC(hdc);
			DeleteDC(hdc0);
			ReleaseDC(IntPtr.Zero, sdc);
			DeleteObject(hbm);
			DeleteObject(hbm0);
			return b0;
		}
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hwnd);
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		public static extern int DeleteDC(IntPtr hdc);
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		public static extern int BitBlt(IntPtr hdcDst, int xDst, int yDst, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
		static int SRCCOPY = 0x00CC0020;
		[System.Runtime.InteropServices.DllImport("gdi32.dll")]
		static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi, uint Usage, out IntPtr bits, IntPtr hSection, uint dwOffset);
		static uint BI_RGB = 0;
		static uint DIB_RGB_COLORS=0;
		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		public struct BITMAPINFO {
			public uint biSize;
			public int biWidth, biHeight;
			public short biPlanes, biBitCount;
			public uint biCompression, biSizeImage;
			public int biXPelsPerMeter, biYPelsPerMeter;
			public uint biClrUsed, biClrImportant;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst=256)]
			public uint[] cols;
		}
		static uint MAKERGB(int r, int g, int b) => ((uint)(b&255)) | ((uint)((r&255)<<8)) | ((uint)((g&255)<<16));
	}
}
