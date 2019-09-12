
namespace libpaperang.Helpers {
	class Transforms {
		public BaseTypes.Packet Frame;
		public BaseTypes.Opcodes Op;
		public Transforms(BaseTypes.Packet FrameConstruction, BaseTypes.Opcodes Operations) {
			Frame=FrameConstruction;
			Op=Operations;
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

	}
}
