using System;
using System.Collections.Generic;
using System.Text;
using LibUsbDotNet;
using Vadavo.NEscPos.Connectors;

namespace libsharperang
{
    public class IUsb : IPrinterConnector
    {
        internal UsbDevice iPrinter;
        private UsbEndpointReader pReader;
        private UsbEndpointWriter pWriter;
        public void Dispose()
        {
            pReader?.Dispose();
            pWriter?.Dispose();
            IUsbDevice p = iPrinter as IUsbDevice;
            _ = p?.ReleaseInterface(0);
            _ = iPrinter?.Close();
            throw new NotImplementedException();
        }

        public byte[] Read()
        {
            if (pReader == null) pReader = iPrinter.OpenEndpointReader(LibUsbDotNet.Main.ReadEndpointID.Ep01);
            byte[] bRead = new byte[1024];
            _ = pReader.Read(bRead, 100, out int _);
            return bRead;
        }

        public void Write(byte[] data)
        {
            if (pWriter == null) pWriter = iPrinter.OpenEndpointWriter(LibUsbDotNet.Main.WriteEndpointID.Ep01);
            pWriter?.Write(data, 100, out int _);
        }
    }
}
