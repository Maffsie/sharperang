using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace libdither {
	public static class ImageTransforms {
		public static Bitmap ConvertTo1Bit(Bitmap input) => input;
		public static byte[] Convert1BitToByteArray(Bitmap input) {
			int hSz=input.Height; int wSz=input.Width;
			byte[] intermed;
			using(MemoryStream ms = new MemoryStream()) {
				input.Save(ms, ImageFormat.Bmp);
				input.Dispose();
				intermed = ms.ToArray();
			}
			byte[] output=new byte[hSz*wSz];
			int hdrOffset=intermed.Length-output.Length;
			for(int h = 0; h < hSz; h++) {
				for(int w = 0; w < wSz; w++) {
					output[(wSz * (hSz - 1 - h)) + (wSz - 1 - w)] =
						(byte)~intermed[hdrOffset + (wSz * h) + (wSz - 1 - w)];
				}
			}
			return output;
		}
	}
}
