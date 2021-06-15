#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for managing a MajorDomo client.
	/// </summary>
	/// 
	public class ClientData
	{
		public string ClientName;
		public string UserName;
		public uint   ClientUID;


		public static readonly uint UID_UNASSIGNED = 0;
		public static readonly uint UID_SERVER     = 1;


		public ClientData(string _clientName, string _userName, uint _uid)
		{
			ClientName = _clientName;
			UserName   = _userName;
			ClientUID  = _uid;
		}

		public ClientData(AUT_WH.MajorDomoProtocol.ClientInformation _information)
		{
			ClientName = _information.ClientName;
			UserName   = _information.UserName;
			ClientUID  = _information.Uid;
		}

		override public string ToString() 
		{ 
			return "'" + ClientName + "' (user='" + UserName + "', cUID=" + ClientUID + ")";
		}
	}
}
