using System.Drawing;

namespace libdither {
	public interface IDither {
		uint BlackPoint { get; set; }
		Bitmap Dither(Bitmap input);
	}
}
