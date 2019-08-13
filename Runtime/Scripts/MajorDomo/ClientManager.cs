#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.MajorDomo
{
	public class ClientManager
	{
		public ClientManager()
		{
			Reset();
		}


		public void Reset()
		{
			clientUidMap = new SortedList<uint, ClientData>();
		}


		public void AddClient(ClientData client)
		{
			if (!clientUidMap.ContainsKey(client.ClientUID))
			{
				clientUidMap[client.ClientUID] = client;
			}
			else
			{
				Debug.LogWarning("Trying to add client with UID=" + client.ClientUID + " again");
			}
		}


		public ClientData GetClientByUID(uint _uid)
		{
			ClientData client = null;
			clientUidMap.TryGetValue(_uid, out client);
			return client;
		}


		public ClientData GetClientByName(string name)
		{
			ClientData entity = null;
			foreach (ClientData e in clientUidMap.Values)
			{
				if ( e.Name == name)
				{
					entity = e;
					break;
				}
			}
			return entity;
		}


		public void RemoveClient(ClientData client)
		{
			if (clientUidMap.ContainsKey(client.ClientUID))
			{
				clientUidMap.Remove(client.ClientUID);
			}
			else
			{
				Debug.LogWarning("Trying to remove unknown client with UID=" + client.ClientUID);
			}
		}


		private SortedList<uint, ClientData> clientUidMap;
	}
}
