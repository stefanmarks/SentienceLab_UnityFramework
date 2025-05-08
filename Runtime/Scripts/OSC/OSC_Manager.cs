//
//	  UnityOSC - Open Sound Control interface for the Unity3d game engine	  
//
//	  Copyright (c) 2012 Jorge Garcia Martin
//
// 	  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// 	  documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// 	  the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// 	  and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// 	  The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// 	  of the Software.
//
// 	  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// 	  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// 	  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// 	  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// 	  IN THE SOFTWARE.
//
//	  Inspired by http://www.unifycommunity.com/wiki/index.php?title=AManagerClass

using SentienceLab.OSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using UnityOSC;

/// <summary>
/// Handles all the OSC servers and clients of the current Unity game/application.
/// Tracks incoming and outgoing messages.
/// </summary>
/// 
[AddComponentMenu("OSC/OSC Manager")]
[DisallowMultipleComponent]
public class OSC_Manager : MonoBehaviour
{
	public int      portIncoming    = 57110;
	public int      portOutgoing    = 57111;
	public string[] startClientList = { "127.0.0.1" };

	[Tooltip("Distribute incoming data to all other registered and connected clients")]
	public bool DistributeData = true;

	[Tooltip("Enable to see output of incoming and outgoing messages")]
	public bool debugDataStream = false;


	public static OSC_Manager Instance
	{
		get { return ms_Instance; }
	}


	/// <summary>
	/// Initializes the OSC Handler.
	/// Here you can create the OSC servers and clientes.
	/// </summary>
	public void Awake()
	{
		if (ms_Instance == null)
		{
			ms_Instance = this;
		}
		else
		{
			Debug.LogWarning("More than one OSC_Manager instances in the scene.");
		}

		// start server
		m_server = new OSCServer(portIncoming);
		m_server.PacketReceivedEvent += OnPacketReceived;

		// prepare clients
		m_clients = new Dictionary<string, Client>();
		foreach (string addr in startClientList)
		{
			m_clients.Add(addr, new Client(new OSCClient(IPAddress.Parse(addr), portOutgoing)));
		}

		// do the variable gathering in the first Update call
		// because some Components might not have had Start() called until now.
		m_variableList    = null;
		m_clientToExclude = null;
	}


	/// <summary>
	/// Ensure that the instance is destroyed properly, closing all ports and clients.
	/// </summary>
	void OnDestroy()
	{
		if (m_server != null)
		{
			m_server.Close();
			m_server = null;
		}

		if (m_clients != null)
		{
			lock (m_clients)
			{
				foreach (Client client in m_clients.Values)
				{
					client.Close();
				}
				m_clients.Clear();
			}
		}
	}


	public void Update()
	{
		// do we need to update the variable list
		if (m_variableList == null)
		{
			// wait one frame so every script has started
			if (Time.frameCount > 1)
			{
				GatherOSC_Variables();
			}
		}
		else
		{
			// run Update on each variable
			foreach (OSC_Variable variable in m_variableList)
			{
				if (variable != null) variable.Update();
			}
		}
	}


	protected void GatherOSC_Variables()
	{
		// gather all OSC variables in the scene
		m_variableList = new List<OSC_Variable>();

		IEnumerable<IOSCVariableContainer> containers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IOSCVariableContainer>();
		foreach (IOSCVariableContainer container in containers)
		{
			m_variableList.AddRange(container.GetOSC_Variables());
		}
		// remove all null/invalid vars
		m_variableList.RemoveAll(v => v == null);
		m_variableList.Sort(OSC_VariableComparer.Instance);

		if (m_variableList.Count > 0)
		{
			StringBuilder varNames = new StringBuilder("OSC Variables:");
			foreach (OSC_Variable variable in m_variableList)
			{
				if (variable != null)
				{
					varNames.Append("\n- '").Append(variable.Name).Append("'");
					variable.SetManager(this);
				}
			}
			Debug.Log(varNames.ToString());
		}

		UpdateAllClients();
	}


	protected void UpdateAllClients()
	{
		foreach (OSC_Variable variable in m_variableList)
		{
			if (variable != null) variable.SendUpdate();
		}
	}


	public void SendPacket(OSCPacket packet)
	{
		if (m_clients == null)
			return;

		if (debugDataStream) DumpPacket("Sending", packet);

		lock (m_clients)
		{
			foreach (Client client in m_clients.Values)
			{
				if (!client.Equals(m_clientToExclude))
				{
					client.Send(packet);
				}
			}
		}
	}


	/// <summary>
	/// Raises the packet received event.
	/// </summary>
	/// <param name="server">the server that needs to process a packet.</param>
	/// <param name="packet">the packet to process.</param>
	/// 
	void OnPacketReceived(OSCServer server, OSCPacket packet)
	{
		if (m_variableList == null) return;

		// check if we have a new client
		string clientAddr = server.LastEndPoint.Address.ToString();
		if (!m_clients.ContainsKey(clientAddr))
		{
			lock (m_clients)
			{
				// Yes: add to the list of addresses to send updates back to
				m_clients.Add(clientAddr, new Client(new OSCClient(IPAddress.Parse(clientAddr), portOutgoing)));
			}
			Debug.Log("Added OSC client " + clientAddr);
			UpdateAllClients();
		}
		else
		{
			// exclude client from receiving its own value
			m_clientToExclude = m_clients[clientAddr];
		}

		if (debugDataStream) DumpPacket("Recevied", packet);

		// check which variable will accept the packet
		foreach (OSC_Variable var in m_variableList)
		{
			if ((var != null) && (var.CanAccept(packet)))
			{
				var.Accept(packet);
				if (DistributeData) { var.SendUpdate(); }
				break;
			}
		}

		m_clientToExclude = null;
	}


	private void DumpPacket(string prefix, OSCPacket packet)
	{
		string output = prefix + " '" + packet.Address + "': [";
		for (int idx = 0; idx < packet.Data.Count; idx++)
		{
			if (idx > 0) output += ", ";
			output += packet.Data[idx].GetType().ToString().Replace("System.", "");
		}
		output += "]";
		Debug.Log(output);
	}


	protected class Client
	{
		public Client(OSCClient _client) 
		{ 
			client    = _client; 
			errors    = 0;
			maxErrors = 10;
		}

		public void Send(OSCPacket _packet)
		{
			if ((client != null) && (errors < maxErrors))
			{
				try
				{
					client.Send(_packet);
					errors = 0;
				}
				catch (Exception e)
				{
					Debug.LogWarning($"Could not send OSC Packet to client {client.ClientIPAddress}:{client.Port}:\n{e}");
					errors++;

					if (errors >= maxErrors)
					{
						Debug.LogWarning($"Too many errors sending OSC Packets to {client.ClientIPAddress}:{client.Port} - Removing Client");
					}
				}
			}
		}

		public void Close()
		{
			client.Close();
		}

		protected OSCClient client;
		protected int       errors;
		protected int       maxErrors;
	}

	protected OSCServer                  m_server;
	protected List<OSC_Variable>         m_variableList;
	protected Dictionary<string, Client> m_clients;
	protected Client                     m_clientToExclude;

	protected static OSC_Manager ms_Instance = null;
}

