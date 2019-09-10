using System;
using Crc;

namespace libsharperang {
	public class DataTransforms {
		public CRC32 hasher;
		private uint MagicNumber=0x35769521;
		public bool IsCrcInitialised() => (hasher!=null && hasher.Initialised);
		public void InitialiseCrc(uint Key = 0x35769521) {
			if (hasher==null || (hasher.Initialised && hasher.Initial != Key)) hasher=new CRC32(Key);
			hasher.Initialise();
		}
		public byte[] GetHashSum(byte[] data) {
			if (!IsCrcInitialised()) InitialiseCrc();
			return BitConverter.GetBytes(hasher.GetChecksum(data));
		}
		public uint GetCrcKey() {
			if (!IsCrcInitialised()) InitialiseCrc();
			return hasher.Initial == MagicNumber ? hasher.Initial : hasher.Initial ^ MagicNumber;
		}
		public byte[] GetCrcKeyBytes() {
			if (!IsCrcInitialised()) InitialiseCrc();
			return BitConverter.GetBytes(GetCrcKey());
		}
		public byte[] GeneratePrintOpcode(byte[] data) => BitConverter.GetBytes(SwapEndianness(
			0x00010000 | (((((
				(uint)data.Length & 0xFFU) << 16) |
				(uint)data.Length) & 0xFFFF00U) >> 8)));
		public uint SwapEndianness(uint value) => (
			(value & 0x000000FFU) << 24) |
			((value & 0x0000FF00U) << 8) |
			((value & 0x00FF0000U) >> 8) |
			((value & 0xFF000000U) >> 24);
	}
}
