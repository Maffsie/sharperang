using System.Drawing;

namespace libdither.Algorithms {
	internal abstract class Base : IDither {
		private uint bwThresh;
		public uint BlackPoint { get => bwThresh; set => bwThresh = value; }
		//Do nothing for the dither method because it's a dummy
		public abstract Bitmap Dither(Bitmap input);
		internal static int Clamp(int input) => Clamp(input, 0, 255);
		internal static int Clamp(int input, int min, int max) => (input < min) ? min : (input > max) ? max : input;

	}
}
