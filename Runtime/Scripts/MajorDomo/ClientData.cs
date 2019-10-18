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
		public string Name;
		public uint   ClientUID;


		public static readonly uint UID_UNASSIGNED = 0;
		public static readonly uint UID_SERVER     = 1;


		public ClientData(string _name, uint _uid)
		{
			Name = _name;
			ClientUID = _uid;
		}

		public ClientData(AUT_WH.MajorDomoProtocol.ClientInformation _information)
		{
			Name = _information.Name;
			ClientUID = _information.Uid;
		}

		override public string ToString() 
		{ 
			return Name + ":" + ClientUID;
		}
	}
}
