using System;
using System.Linq;

namespace libpaperang.Helpers {
	public class Transforms {
		public BaseTypes.Packet Frame;
		public BaseTypes.Opcodes Op;
		public short OpLen;
		public Transforms(BaseTypes.Packet FrameConstruction, BaseTypes.Opcodes Operations, short OperLen) {
			Frame=FrameConstruction;
			Op=Operations;
			OpLen=OperLen;
		}
		public byte[] Oper(BaseTypes.Operations Operation) {
			switch (Operation) {
				case BaseTypes.Operations.NoOp: return Op.NoOp;
				case BaseTypes.Operations.LineFeed: return Op.LineFeed;
				case BaseTypes.Operations.CrcTransmit: return Op.TransmitCrc;
				case BaseTypes.Operations.Print: return Op.Print;
				default: throw new InvalidOperationException();
			}
		}
		public byte[] Arg(BaseTypes.Operations Operation, uint Data) {
			switch(Operation) {
				case BaseTypes.Operations.LineFeed: return BitConverter.GetBytes(SwapWordEndianness(
					0x00000000 | (((((
						Data & 0xffu) << 16) |
						Data) & 0xffff00u) >> 8))).Skip(2).ToArray();
				case BaseTypes.Operations.Print:
					return BitConverter.GetBytes(SwapWordEndianness(
						0x00010000 | (((((
						Data & 0xffu) << 16) |
						Data) & 0xffff00u) >> 8)));
				default: throw new InvalidOperationException();
			}
		}
		public byte[] Packet(byte[] oper, byte[] data, CRC checksum) {
			byte[] packet = new byte[1+oper.Length+data.Length+5];
			packet[0] = Frame.Start;
			packet[packet.Length-1] = Frame.End;
			Buffer.BlockCopy(oper,
				0, packet, 1, oper.Length);
			Buffer.BlockCopy(data,
				0, packet, oper.Length+1, data.Length);
			Buffer.BlockCopy(checksum.GetChecksumBytes(data),
				0, packet, packet.Length - 5, 4);
			return packet;
		}
		public byte[] Packet(BaseTypes.Operations oper, byte[] data, CRC checksum) =>
			Packet(Oper(oper), data, checksum);
		public byte[] Packet(BaseTypes.Operations oper, byte[] data, CRC checksum, byte[] operarg) {
			byte[] p=Packet(oper, data, checksum);
			Buffer.BlockCopy(operarg, 0, p, 3, 2);
			return p;
		}
		public uint SwapWordEndianness(uint value) => (
			(value & 0x000000ffu)  << 24)|
			((value & 0x0000ff00u) << 8) |
			((value & 0x00ff0000u) >> 8) |
			((value & 0xff000000u) >> 24);
		public uint SwapByteEndianness(uint value) =>
			Swap4BitEndianness(
				Swap2BitEndianness(
					Swap1BitEndianness(value)));
		public uint Swap4BitEndianness(uint value) => (
			(value & 0x0f0f0f0fu) << 4) |
			((value & (~0x0f0f0f0fu)) >> 4);
		public uint Swap2BitEndianness(uint value) => (
			(value & 0x33333333u) << 2) |
			((value & (~0x33333333u)) >> 2);
		public uint Swap1BitEndianness(uint value) => (
			(value & 0x55555555u) << 1) |
			((value & (~0x55555555u)) >> 1);
	}
}
