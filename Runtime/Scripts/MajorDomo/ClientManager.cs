#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System.Collections.Generic;
using System.Text;
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
			m_clientUidMap = new SortedList<uint, ClientData>();
		}


		public void AddClient(ClientData _client)
		{
			if (!m_clientUidMap.ContainsKey(_client.ClientUID))
			{
				m_clientUidMap[_client.ClientUID] = _client;
			}
			else
			{
				Debug.LogWarning("Trying to add client with UID=" + _client.ClientUID + " again");
			}
		}


		public ClientData GetClientByUID(uint _uid)
		{
			ClientData client = null;
			m_clientUidMap.TryGetValue(_uid, out client);
			return client;
		}


		public ClientData GetClientByName(string _clientName)
		{
			ClientData entity = null;
			foreach (ClientData e in m_clientUidMap.Values)
			{
				if ( e.ClientName == _clientName)
				{
					entity = e;
					break;
				}
			}
			return entity;
		}


		public void RemoveClient(ClientData _client)
		{
			if (m_clientUidMap.ContainsKey(_client.ClientUID))
			{
				m_clientUidMap.Remove(_client.ClientUID);
			}
			else
			{
				Debug.LogWarning("Trying to remove unknown client with UID=" + _client.ClientUID);
			}
		}


		public static string ClientListAsString(IReadOnlyList<ClientData> _clientList)
		{
			StringBuilder sb = new StringBuilder();
			int idx = 1;
			foreach (var client in _clientList)
			{
				if (idx > 1) sb.Append('\n');
				sb.Append(idx).Append(":\t").Append(client.ToString());
				idx++;
			}
			return sb.ToString();
		}


		private SortedList<uint, ClientData> m_clientUidMap;
	}
}
