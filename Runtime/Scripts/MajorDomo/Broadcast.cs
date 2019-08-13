#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for managing a Broadcast.
	/// </summary>
	/// 
	public class Broadcast
	{
		public readonly ClientData client;
		public readonly uint       identifier;
		public readonly byte[]     data;


		public Broadcast(ClientData _client, AUT_WH.MajorDomoProtocol.ServerEvent_ClientBroadcast _information)
		{
			client     = _client;
			identifier = _information.Identifier;
			var srcData = _information.GetDataBytes().Value;
			data = new byte[srcData.Count];
			System.Array.Copy(srcData.Array, srcData.Offset, data, 0, data.Length);
		}

		public string GetDataAsString()
		{
			return System.Text.Encoding.UTF8.GetString(data);
		}
	}
}
