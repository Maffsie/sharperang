using System;
using Crc;

namespace libsharperang {
	public class DataTransforms {
		public class CrcSum : Crc32Base {
			public uint CrcKey;
			public CrcSum(uint Mangled, uint Key) : base(0x04c11db7, Mangled, 0xffffffff, true, true) => CrcKey=Key;
		}
		private uint Bludgeon(uint iv) {
			uint ivr=0;
			for (int i=0;i<32;i++) {
				uint bit=(iv>>i)&1;
				ivr |= (bit<<(31-i));
			}
			return ~ivr;
		}
		public CrcSum CRC;
		public bool IsCrcInitialised() => (CRC!=null);
		public void InitialiseCrc() => CRC=new CrcSum(Bludgeon(0x77c40d4d^0x35769521), 0x77c40d4d);
		public void InitialiseCrc(uint Key) => CRC=new CrcSum(Bludgeon(Key), Key);
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
