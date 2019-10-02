using System.Drawing;

namespace libdither.Algorithms {
	internal abstract class Ordered : Base {
		private readonly byte[,] matrix;
		private readonly byte hSzMx;
		private readonly byte wSzMx;
		internal protected Ordered(byte[,] input) {
			hSzMx = (byte)(input.GetUpperBound(1) + 1);
			wSzMx = (byte)(input.GetUpperBound(0) + 1);
			int maxMx=wSzMx*hSzMx;
			int scMx=255/maxMx;
			matrix = new byte[hSzMx, wSzMx];
			for(int x = 0; x < wSzMx; x++)
				for(int y = 0; y < hSzMx; y++)
					matrix[x, y] = (byte)Clamp(input[x, y] * scMx);
		}
		public override Bitmap Dither(Bitmap input) {
			int rC; int cC; byte tC;
			cC
		}
	}
}
