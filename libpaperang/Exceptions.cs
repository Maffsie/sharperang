using System;

namespace libpaperang {

	public class PrinterNotInitialisedException : InvalidOperationException { }
	public class PrinterNotAvailableException : NullReferenceException { }
	public class PrinterVariantNotSupportedException : NotSupportedException { }
	public class CrcNotAvailableException : MissingMemberException { }
	public class InvalidOperationException : ArgumentOutOfRangeException { }
}
