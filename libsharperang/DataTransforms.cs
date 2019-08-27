using System;
using Crc;

namespace libsharperang {
	public class DataTransforms {
		/*public class CrcSum : Crc32Base {
			public uint CrcKey;
			public CrcSum(uint Mangled, uint Key) : base(0x04c11db7, Mangled, 0xffffffff, true, true) => CrcKey=Key;
		}
		
		public CrcSum CRC;*/
		public CRC32 hasher;
		private uint MagicNumber=0x35769521;
		//public bool IsCrcInitialised() => (CRC!=null);
		public bool IsCrcInitialised() => (hasher!=null && hasher.Initialised);
		//public void InitialiseCrc() => CRC=new CrcSum(Bludgeon(0x77c40d4d^0x35769521), 0x77c40d4d);
		//public void InitialiseCrc(uint Key) => CRC=new CrcSum(Bludgeon(Key), Key);
		public void InitialiseCrc(uint Key=0x35769521) {
			if (hasher==null || (hasher.Initialised && hasher.Initial != Key)) hasher=new CRC32(Key);
			hasher.Initialise();
		}
		public byte[] GetHashSum(byte[] data) {
			if (!IsCrcInitialised()) InitialiseCrc();
			//return CRC.ComputeHash(data);
			return BitConverter.GetBytes(hasher.GetChecksum(data));
		}
		public uint GetCrcKey() {
			if (!IsCrcInitialised()) InitialiseCrc();
			//return CRC.CrcKey;
			return (hasher.Initial == MagicNumber? hasher.Initial : hasher.Initial ^ MagicNumber);
		}
		public byte[] GetCrcKeyBytes() {
			if (!IsCrcInitialised()) InitialiseCrc();
			return BitConverter.GetBytes(GetCrcKey());
		}
		public uint SwapEndianness(uint value) => (
			(value & 0x000000FFU) << 24) |
			((value & 0x0000FF00U) << 8) |
			((value & 0x00FF0000U) >> 8) |
			((value & 0xFF000000U) >> 24);
	}
}
