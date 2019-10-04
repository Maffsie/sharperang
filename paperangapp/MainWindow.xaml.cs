using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using liblogtiny;
using libpaperang;
using libpaperang.Interfaces;
using libpaperang.Main;

namespace paperangapp {
	public partial class MainWindow : Window {
		//TODO memory optimisation - this thing is a hongery boi
		private ILogTiny logger;
		private BaseTypes.Connection mmjcx=BaseTypes.Connection.USB;
		private BaseTypes.Model mmjmd=BaseTypes.Model.None;
		private IPrinter prtr=new USB(BaseTypes.Model.None);
		private Paperang mmj=null;
		private System.Timers.Timer usbpoll;
		byte[,] bayer2 = {
			{ 0, 2 },
			{ 3, 1 }
		};
		byte[,] bayer3 = {
			{ 0, 7, 3 },
			{ 6, 5, 2 },
			{ 4, 1, 8 }
		};
		byte[,] bayer4 = {
			{ 0,  8,  2, 10 },
			{ 12, 4, 14,  6 },
			{ 3, 11,  1,  9 },
			{ 15, 7, 13,  5 }
		};
		byte[,] bayer8 = {
			{  0, 48, 12, 60,  3, 51, 15, 63 },
			{ 32, 16, 44, 28, 35, 19, 47, 31 },
			{  8, 56,  4, 52, 11, 59,  7, 55 },
			{ 40, 24, 36, 20, 43, 27, 39, 23 },
			{  2, 50, 14, 62,  1, 49, 13, 61 },
			{ 34, 18, 46, 30, 33, 17, 45, 29 },
			{ 10, 58,  6, 54,  9, 57,  5, 53 },
			{ 42, 26, 38, 22, 41, 25, 37, 21 }
		};
		//private uint dThresh=127;
		private enum AppState {
			UnInitNoDev,
			UnInitDev,
			InitDev
		};
		private AppState state=AppState.UnInitDev;
		// TODO: is it out of scope for this library to provide functionality for printing bitmap data?
		private byte Clamp(int v) => Convert.ToByte(v < 0 ? 0 : v > 255 ? 255 : v);
		public MainWindow() {
			InitializeComponent();
			logger = new LUITextbox();
			gMain.DataContext = (LUITextbox)logger;
			logger.Info("Application started");
			usbpoll = new System.Timers.Timer(200) {
				AutoReset = true
			};
			usbpoll.Elapsed += EvtUsbPoll;
			usbpoll.Start();
			logger.Verbose("USB presence interval event started");
			byte _szW; byte _szH; int _szM; int _sc;
			//bayer2
			_szW = (byte)(bayer2.GetUpperBound(1) + 1);
			_szH = (byte)(bayer2.GetUpperBound(0) + 1);
			_szM = _szW * _szH;
			_sc = 255 / _szM;
			for(int mx = 0; mx < _szW; mx++) {
				for(int my = 0; my < _szH; my++)
					bayer2[mx, my] = Clamp(bayer2[mx, my] * _sc);
			}

			//bayer3
			_szW = (byte)(bayer3.GetUpperBound(1) + 1);
			_szH = (byte)(bayer3.GetUpperBound(0) + 1);
			_szM = _szW * _szH;
			_sc = 255 / _szM;
			for(int mx = 0; mx < _szW; mx++) {
				for(int my = 0; my < _szH; my++)
					bayer3[mx, my] = Clamp(bayer3[mx, my] * _sc);
			}

			//bayer4
			_szW = (byte)(bayer4.GetUpperBound(1) + 1);
			_szH = (byte)(bayer4.GetUpperBound(0) + 1);
			_szM = _szW * _szH;
			_sc = 255 / _szM;
			for(int mx = 0; mx < _szW; mx++) {
				for(int my = 0; my < _szH; my++)
					bayer4[mx, my] = Clamp(bayer4[mx, my] * _sc);
			}

			//bayer8
			_szW = (byte)(bayer8.GetUpperBound(1) + 1);
			_szH = (byte)(bayer8.GetUpperBound(0) + 1);
			_szM = _szW * _szH;
			_sc = 255 / _szM;
			for(int mx = 0; mx < _szW; mx++) {
				for(int my = 0; my < _szH; my++)
					bayer8[mx, my] = Clamp(bayer8[mx, my] * _sc);
			}
		}

		private void EvtUsbPoll(object sender, ElapsedEventArgs e) => _ = Dispatcher.BeginInvoke(new invDgtUsbPoll(DgtUsbPoll));
		private delegate void invDgtUsbPoll();
		private void DgtUsbPoll() {
			try {
				if(state == AppState.UnInitNoDev && prtr.PrinterAvailable) {
					btInitUSB.IsEnabled = true;
					state = AppState.UnInitDev;
					logger.Info("Printer plugged in");
				} else if(state == AppState.UnInitDev && !prtr.PrinterAvailable) {
					btInitUSB.IsEnabled = false;
					state = AppState.UnInitNoDev;
					logger.Info("Printer unplugged");
				} else if(state == AppState.InitDev && !prtr.PrinterAvailable) {
					logger.Info("Printer unplugged while initialised");
					USBDeInit();
				}

			} catch(Exception) {
			} finally { }
		}

		~MainWindow() {
			logger.Warn("Application closing");
			if(mmj != null)
				USBDeInit();
		}
		private void BtClearLog_Click(object sender, RoutedEventArgs e) =>
			logger.Raw("!clearlog");
		private void SetP1_Click(object sender, RoutedEventArgs e) {
			mmjmd = BaseTypes.Model.P1;
			logger.Info("Model type set to P1 or P1S");
		}
		private void SetP2_Click(object sender, RoutedEventArgs e) {
			mmjmd = BaseTypes.Model.P2;
			logger.Info("Model type set to P2 or P2S");
		}
		private void SetT1_Click(object sender, RoutedEventArgs e) {
			mmjmd = BaseTypes.Model.T1;
			logger.Info("Model type set to T1");
		}
		private void BtInitUSB_Click(object sender, RoutedEventArgs e) => USBInit();
		private void USBInit() {
			mmj = new Paperang(mmjcx, mmjmd);
			mmj.SetLogContext(logger);
			logger.Verbose("# printers found: " + mmj.Printer.AvailablePrinters.Count);
			if(!mmj.Printer.PrinterAvailable) {
				logger.Error("Couldn't initialise printer as none is connected");
				return;
			}
			logger.Info("USB Initialising");
			mmj.Initialise();
			logger.Debug("PrinterInitialised? " + mmj.Printer.PrinterInitialised);
			logger.Debug("Printer initialised and ready");
			state = AppState.InitDev;
			btInitUSB.IsEnabled = false;
			btDeInitUSB.IsEnabled = true;
			gbOtherFunc.IsEnabled = true;
			gbPrinting.IsEnabled = true;
		}
		private void BtDeInitUSB_Click(object sender, RoutedEventArgs e) => USBDeInit();
		private void USBDeInit() {
			logger.Info("De-initialising printer");
			mmj.Printer.ClosePrinter();
			mmj.Printer.Deinitialise();
			mmj = null;
			state = AppState.UnInitDev;
			try {
				gbPrinting.IsEnabled = false;
				gbOtherFunc.IsEnabled = false;
				btInitUSB.IsEnabled = true;
				btDeInitUSB.IsEnabled = false;
			} catch(Exception) {
			} finally { }
		}
		private async void BtFeed_Click(object sender, RoutedEventArgs e) => await mmj.FeedAsync((uint)slFeedTime.Value);
		private async void BtPrintText_Click(object sender, RoutedEventArgs e) {
			if(!(txInput.Text.Length > 0)) {
				logger.Warn("PrintText event but nothing to print.");
				return;
			}
			await PrintTextAsync(txInput.Text, txFont.Text, int.Parse(txSzF.Text));
		}
		private async Task PrintTextAsync(string text, string font, int szf) {
			Font fnt=new Font(font, szf);
			TextFormatFlags tf=
				TextFormatFlags.Left |
				TextFormatFlags.NoPadding |
				TextFormatFlags.NoPrefix |
				TextFormatFlags.Top |
				TextFormatFlags.WordBreak;
			System.Drawing.Size szText = TextRenderer.MeasureText(text, fnt, new System.Drawing.Size(mmj.Printer.LineWidth*8,10000), tf);
			Bitmap b=new Bitmap(mmj.Printer.LineWidth*8, szText.Height);
			Graphics g = Graphics.FromImage(b);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
			TextRenderer.DrawText(g, text, fnt, new System.Drawing.Point(0, 0), Color.Black, tf);
			g.Flush();
			await Task.Run(() => PrintBitmap(b, false));
			g.Dispose();
			b.Dispose();
		}
		private async void BtPrintImage_Click(object sender, RoutedEventArgs e) => await PrintImageAsync();
		private async Task PrintImageAsync() {
			logger.Debug("Invoking file selection dialogue.");
			OpenFileDialog r = new OpenFileDialog {
				Title="Select 1 (one) image file",
				Multiselect=false,
				Filter="PNG files|*.png|JPEG files|*.jpg;*.jpeg|Jraphics Interchange Format files|*.gif|Bitte-Mappe files|*.bmp|All of the above|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
				AutoUpgradeEnabled=true
			};
			if(r.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) {
				logger.Warn("PrintImage event but no file was selected.");
				r.Dispose();
				return;
			}
			string fn=r.FileName;
			r.Dispose();
			Bitmap bmg = await Task.Run(() => {
				logger.Debug($"Loading image '{fn}' for print");
				Image _=Image.FromFile(fn);
				logger.Debug($"Loaded image '{fn}'");
				logger.Debug("Disposed of dialog");
				Bitmap bimg=new Bitmap(_, mmj.Printer.LineWidth*8,
				(int)(mmj.Printer.LineWidth*8*((double)_.Height/_.Width)));
				logger.Debug("Loaded image as Bitmap");
				_.Dispose();
				logger.Debug("Disposed of Image");
				return bimg;
			});
			await Task.Run(() => PrintBitmap(bmg));
			bmg.Dispose();
		}
		private async Task PrintBitmap(Bitmap bimg, bool dither = true) {
			if(dither) {
				logger.Trace("Dithering input bitmap");
				bimg = AForge.Imaging.Filters.Grayscale.CommonAlgorithms.Y.Apply(bimg);
				AForge.Imaging.Filters.OrderedDithering f = new
				AForge.Imaging.Filters.OrderedDithering(bayer4);
				//f.FormatTranslations.Clear();
				//f.FormatTranslations[PixelFormat.Format1bppIndexed] = PixelFormat.Format1bppIndexed;
				bimg = f.Apply(bimg);
				//bimg = new Accord.Imaging.Filters.BayerDithering().Apply(bimg);
				logger.Debug("Dithered Bitmap");
			}
			bimg = CopyToBpp(bimg);
			logger.Debug("Converted Bitmap to 1-bit");
			int hSzImg=bimg.Height;
			byte[] iimg = new byte[hSzImg*mmj.Printer.LineWidth];
			byte[] img;
			using(MemoryStream s = new MemoryStream()) {
				bimg.Save(s, ImageFormat.Bmp);
				img = s.ToArray();
			}
			logger.Debug("Got bitmap's bytes");
			bimg.Dispose();
			logger.Debug("Disposed of Bitmap");
			int startoffset=img.Length-(hSzImg*mmj.Printer.LineWidth);
			logger.Debug("Processing bytes with offset " + startoffset);
			for(int h = 0; h < hSzImg; h++) {
				for(int w = 0; w < mmj.Printer.LineWidth; w++) {
					iimg[(mmj.Printer.LineWidth * (hSzImg - 1 - h)) + (mmj.Printer.LineWidth - 1 - w)] = (byte)~
						img[startoffset + (mmj.Printer.LineWidth * h) + (mmj.Printer.LineWidth - 1 - w)];
				}
			}
			logger.Debug($"Have {img.Length} bytes of print data ({mmj.Printer.LineWidth * 8}x{hSzImg}@1bpp)");
			await mmj.PrintBytesAsync(iimg, false);
		}
		#region GDIBitmap1bpp
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
			bmi.biClrUsed = 2;
			bmi.biClrImportant = 2;
			bmi.cols = new uint[256];
			bmi.cols[0] = MAKERGB(0, 0, 0);
			bmi.cols[1] = MAKERGB(255, 255, 255);
			IntPtr hbm0 = CreateDIBSection(IntPtr.Zero,ref bmi,DIB_RGB_COLORS,out _,IntPtr.Zero,0);
			IntPtr sdc = GetDC(IntPtr.Zero);
			IntPtr hdc = CreateCompatibleDC(sdc);
			_ = SelectObject(hdc, hbm);
			IntPtr hdc0 = CreateCompatibleDC(sdc);
			_ = SelectObject(hdc0, hbm0);
			_ = BitBlt(hdc0, 0, 0, w, h, hdc, 0, 0, SRCCOPY);
			Bitmap b0 = Image.FromHbitmap(hbm0);
			_ = DeleteDC(hdc);
			_ = DeleteDC(hdc0);
			_ = ReleaseDC(IntPtr.Zero, sdc);
			_ = DeleteObject(hbm);
			_ = DeleteObject(hbm0);
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
		static uint MAKERGB(int r, int g, int b) => ((uint)(b & 255)) | ((uint)((r & 255) << 8)) | ((uint)((g & 255) << 16));
		#endregion
	}
}
