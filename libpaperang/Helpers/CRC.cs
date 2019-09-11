using System;
using System.Collections.Generic;
using System.Linq;

namespace libpaperang.Helpers {
	public class CRC {
		public uint iv { get => ~iv; private set => iv=~value; }
		public uint mv = 0x35769521;
		private uint poly = 0xedb88320;
		private uint[] crctable;
		public bool IsInitialised=false;
		public CRC() { this.iv=0; }
		public CRC(uint iv) => this.iv=iv;
		public void Initialise() {
			crctable=Enumerable.Range(0, 256).Select(i => {
				uint e=(uint)i;
				for (ushort eb = 0; eb<8; eb++)
					e=((e&1)!=0)
						? (poly^(e>>1))
						: (e>>1);
				return e;
			}).ToArray();
			IsInitialised=true;
		}
		public uint Reflect(uint iv) {
			uint ivr=0;
			for (int i = 0; i<32; i++) {
				uint b=(iv>>i)&1;
				ivr|=(b<<(31-i));
			}
			return ~ivr;
		}
		public uint GetChecksumUint<T>(IEnumerable<T> data) {
			if (!IsInitialised) throw new InvalidOperationException("CRC object has not been Initialise()'d first. Please do that.");
			try {
				return ~data.Aggregate(iv,
					(cti, cb) => crctable[(cti&0xFF)^Convert.ToByte(cb)]^(cti>>8));
			} catch (FormatException e) {
				throw new FormatException("Could not read input as a byte stream", e);
			} catch (InvalidCastException e) {
				throw new InvalidCastException("Could not read input as a byte stream", e);
			} catch (OverflowException e) {
				throw new OverflowException("Could not read input as a byte stream", e);
			}
		}
		public byte[] GetChecksumBytes<T>(IEnumerable<T> data) => BitConverter.GetBytes(GetChecksumUint(data));
		public uint GetCrcIv() => iv == mv
																? iv
																: iv ^ mv;
		public byte[] GetCrcIvBytes() => BitConverter.GetBytes(GetCrcIv());
	}
}
