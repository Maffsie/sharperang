﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace libsharperang {
	public class CRC32 {
		public uint Initial { get; private set; } = 0xFFFFFFFF;
		private uint Polynomial = 0xedb88320;
		//private uint Polynomial = 0x04c11db7;
		private uint[] Table;
		public bool Initialised=false;
		public CRC32(uint Initial) => this.Initial=Initial;
		public CRC32() { }
		public void Initialise() {
			Table=Enumerable.Range(0, 256).Select(i => {
				uint e=(uint)i;
				for (ushort eb = 0; eb<8; ++eb) {
					e=((e&1)!=0)
						? (Polynomial^(e>>1))
						: (e>>1);
				}
				return e;
			}).ToArray();
			Initialised=true;
		}
		public uint Reflect(uint iv) {
			uint ivr=0;
			for (int i = 0; i<32; i++) {
				uint bit=(iv>>i)&1;
				ivr |= (bit<<(31-i));
			}
			return ~ivr;
		}
		public uint GetChecksum<T>(IEnumerable<T> bytes) {
			if (!Initialised) Initialise();
			try {
				return ~bytes.Aggregate(Initial,
					(csR, cB) => Table[(csR & 0xFF) ^ Convert.ToByte(cB)] ^ (csR>>8));
			} catch (FormatException e) {
				throw new Exception("Could not read stream as bytes", e);
			} catch (InvalidCastException e) {
				throw new Exception("Could not read stream as bytes", e);
			} catch (OverflowException e) {
				throw new Exception("Could not read stream as bytes", e);
			}
		}
	}
}
