
namespace libsharperang {
	public abstract class Base {
		public enum ConnectionType {
			None,
			UART,
			USB,
			Bluetooth
		}
		internal DataTransforms transform=new DataTransforms();
		public string Model { get; internal set; }
		public string FirmwareVer { get; internal set; }
		public int Battery { get; internal set; }
		public int ImageWidth { get; internal set; }
		public ConnectionType ActiveConnectionType { get; internal set; } = ConnectionType.None;
		internal bool InitialiseConnection() => false;
		internal bool DestroyConnection() => false;
		public Base() => InitialiseConnection();
		~Base() => DestroyConnection();
	}
}
