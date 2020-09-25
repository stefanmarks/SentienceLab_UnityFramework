#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for managing information about a MajorDomo server.
	/// </summary>
	/// 
	public class ServerInformation
	{
		public readonly string name;                    // name of the server
		public readonly string address;                 // network adddress or hostname of the server

		public readonly VersionNumber serverVersion;    // version number of the server
		public readonly VersionNumber protocolVersion;  // version number of the protocol

		public readonly ushort clientRequestPort;       // port for client requests
		public readonly ushort serverEventPort;         // port for receiving server events
		public readonly ushort clientUpdatePort;        // port for sending client updates
		public readonly ushort serverUpdatePort;        // port for receiving server updates

		public readonly ushort updateInterval;          // interval for server-side updates in milliseconds
		public readonly ushort clientHeartbeatInterval; // minimum heartbeat interval in milliseconds for clients

		public readonly bool   allowsRemoteControl;     // can server be remote-controlled?


		public ServerInformation()
		{
			name    = "";
			address = "";
			serverVersion   = new VersionNumber();
			protocolVersion = new VersionNumber();

			clientRequestPort = 0;
			serverEventPort   = 0;
			clientUpdatePort  = 0;
			serverUpdatePort  = 0;

			updateInterval          = 0;
			clientHeartbeatInterval = 0;

			allowsRemoteControl = false;
		}


		public ServerInformation(AUT_WH.MajorDomoProtocol.ServerInformation _information, string _serverAddress, ushort _serverPort)
		{
			name    = _information.Name;
			address = _serverAddress;

			serverVersion   = new VersionNumber(_information.ServerVersion.Value);
			protocolVersion = new VersionNumber(_information.ProtocolVersion.Value);

			clientRequestPort = _serverPort;
			serverEventPort   = _information.ServerEventPort;
			clientUpdatePort  = _information.ClientUpdatePort;
			serverUpdatePort  = _information.ServerUpdatePort;

			updateInterval          = _information.UpdateInterval;
			clientHeartbeatInterval = _information.ClientHeartbeatInterval;

			allowsRemoteControl     = _information.AllowsRemoteControl;
		}
	}
}
