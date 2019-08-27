using System;
using Crc;

namespace libsharperang {
	public class DataTransforms {
		public class CrcSum : Crc32Base {
			public uint CrcKey;
			public CrcSum() : base(0x77c40d4d^0x35769521, 0xffffffff, 0xffffffff, false, false) => CrcKey=0x77c40d4d;
			public CrcSum(uint Key) : base(Key^0x35769521, 0xffffffff, 0xffffffff, false, false) => CrcKey=Key;
			public CrcSum(uint Key, bool _) : base(Key, 0xffffffff, 0xffffffff, false, false) => CrcKey=Key;
		}
		public CrcSum CRC;
		public bool IsCrcInitialised() => (CRC!=null);
		public void InitialiseCrc() => CRC=new CrcSum();
		public void InitialiseCrc(uint Key) => CRC=new CrcSum(Key);
		public void InitialiseCrc(uint Key, bool _) => CRC=new CrcSum(Key, _);
		public uint GetCrcKey() {
			if (!IsCrcInitialised()) InitialiseCrc();
			return CRC.CrcKey;
		}
		public byte[] GetCrcKeyBytes() {
			if (!IsCrcInitialised()) InitialiseCrc();
			return BitConverter.GetBytes(GetCrcKey());
		}
	}
}
