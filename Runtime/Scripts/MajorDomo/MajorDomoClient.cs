#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.MajorDomo
{
	/// <summary>
	/// Class for connecting as a client to a MajorDomo server. 
	/// </summary>
	/// 
	public class MajorDomoClient
	{
		// Client Version number
		public const int VERSION_MAJOR    = 0;
		public const int VERSION_MINOR    = 4;
		public const int VERSION_REVISION = 8;


		public ClientManager ClientManager { get; private set; }
		public EntityManager EntityManager { get; private set; }


		public delegate void ClientRegistered(ClientData _client);
		public event ClientRegistered OnClientRegistered;

		public delegate void ClientUnregistered(ClientData _client);
		public event ClientUnregistered OnClientUnregistered;

		public delegate void EntitiesPublished(List<EntityData> _entities);
		public event EntitiesPublished OnEntitiesPublished;

		public delegate void EntitiesRevoked(List<EntityData> _entities);
		public event EntitiesRevoked OnEntitiesRevoked;

		public delegate void EntityControlChanged(List<EntityData> _entities);
		public event EntityControlChanged OnEntityControlChanged;

		public delegate void ClientBroadcastReceived(Broadcast _broadcast);
		public event ClientBroadcastReceived OnClientBroadcastReceived;

		public delegate void ServerShutdown(bool _reboot);
		public event ServerShutdown OnServerShutdown;


		public MajorDomoClient()
		{
			m_startTime = DateTime.Now;

			// Necessary to force .NET implementation of network functions
			// Client will most likely hang without this line!!!!!
			AsyncIO.ForceDotNet.Force();

			m_client = null;

			ClientManager = new ClientManager();
			EntityManager = new EntityManager();

			m_bufOut = new FlatBuffers.FlatBufferBuilder(1024);
			m_msgOut = new NetMQ.Msg();
			m_msgIn  = new NetMQ.Msg();
			m_serverReply   = new AUT_WH.MajorDomoProtocol.ServerReply();
			m_serverEvent   = new AUT_WH.MajorDomoProtocol.ServerEvent();
			m_entityUpdates = new AUT_WH.MajorDomoProtocol.EntityUpdates();

			m_clientRequestSocket = null;
			m_serverEventSocket   = null;
			m_clientUpdateSocket  = null;
			m_serverUpdateSocket  = null;

			m_updatedEntities = new List<EntityData>();

			m_lastUpdateSent = DateTime.Now;
		}


		public bool Connect(string _applicationName, string _serverAddress, ushort _serverPort, float _timeout, bool _retrieveLists = true)
		{
			bool success = false;

			if (!IsConnected())
			{
				string address = "tcp://" + _serverAddress + ":" + _serverPort;
				Debug.LogFormat(
					"Trying to connect to MajorDomo server at '{0}'...", 
					address);
				try
				{
					m_clientRequestSocket = new NetMQ.Sockets.RequestSocket();
					m_clientRequestSocket.SetSocketOption(NetMQ.Core.ZmqSocketOption.SendTimeout, (int) (_timeout * 1000));
					m_clientRequestSocket.Connect(address);
					m_timeoutInterval = _timeout;
				}
				catch (Exception e)
				{
					Debug.LogWarningFormat(
						"Could not connect to MajorDomo server at '{0}': {1}",
						address, e.Message);
					Cleanup();
					return false;
				}

				Debug.Log("Sending connection request...");

				// build request
				m_bufOut.Clear();
				var clientName = m_bufOut.CreateString(_applicationName);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.StartClReq_ClientConnect(m_bufOut);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.AddClientName(m_bufOut, clientName);
				var clientVersion = AUT_WH.MajorDomoProtocol.Version.CreateVersion(m_bufOut,
					VERSION_MAJOR,
					VERSION_MINOR,
					VERSION_REVISION);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.AddClientVersion(m_bufOut, clientVersion);
				var clientProtocol = AUT_WH.MajorDomoProtocol.Version.CreateVersion(m_bufOut,
					(sbyte)AUT_WH.MajorDomoProtocol.EProtocolVersion.MAJOR,
					(sbyte)AUT_WH.MajorDomoProtocol.EProtocolVersion.MINOR,
					(ushort)AUT_WH.MajorDomoProtocol.EProtocolVersion.REVISION);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.AddClientProtocol(m_bufOut, clientProtocol);
				var reqClientConnect = AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.EndClReq_ClientConnect(m_bufOut);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ClientConnect, reqClientConnect.Value))
				{
					if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_ClientConnect)
					{
						var ack = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_ClientConnect>().Value;
						Debug.Log("Connected to MajorDomo server '" + ack.ServerName + "'" +
							" v" + ack.ServerVersion.Value.NumMajor +
							"." + ack.ServerVersion.Value.NumMinor +
							"." + ack.ServerVersion.Value.NumRevision +
							" (protocol v" + ack.ServerProtocol.Value.NumMajor +
							"." + ack.ServerProtocol.Value.NumMinor +
							"." + ack.ServerProtocol.Value.NumRevision +
							", server time " + m_serverReply.Timestamp +
							") with Client UID " + ack.ClientUID);

						m_client = new ClientData(_applicationName, ack.ClientUID);
						EntityManager.SetClientUID(ack.ClientUID);

						// TODO: Catch any exceptions happening during the following three connections

						// create other connections for:
						// server events
						string serverEventSocket = "tcp://" + _serverAddress + ":" + ack.ServerEventPort;
						Debug.LogFormat("Server event socket: {0}", serverEventSocket);
						m_serverEventSocket = new NetMQ.Sockets.SubscriberSocket(serverEventSocket);
						m_serverEventSocket.SubscribeToAnyTopic();
						m_serverEventSocket.ReceiveReady += ServerEvent_ReceiveReady;
					
						// Client updates
						string clientUpdateSocket = "tcp://" + _serverAddress + ":" + ack.ClientUpdatePort;
						Debug.LogFormat("Client update socket: {0}", clientUpdateSocket);
						m_clientUpdateSocket = new NetMQ.Sockets.PushSocket(clientUpdateSocket);
					
						// Server updates
						string serverUpdateSocket = "tcp://" + _serverAddress + ":" + ack.ServerUpdatePort;
						Debug.LogFormat("Server update socket: {0}", serverUpdateSocket);
						m_serverUpdateSocket = new NetMQ.Sockets.SubscriberSocket(serverUpdateSocket);
						m_serverUpdateSocket.SubscribeToAnyTopic();
						m_serverUpdateSocket.ReceiveReady += ServerUpdate_ReceiveReady;

						// get interval for sending heartbeat signal
						m_heartbeatInterval = ack.HeartbeatInterval / 1000.0f;

						m_entityListRetrieved = false;
						
						success = true;

						if (_retrieveLists)
						{
							RetrieveClientList();
							RetrieveEntityList();
						}

						OnClientRegistered?.Invoke(m_client);
					}
					else if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_Error)
					{
						var error = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_Error>().Value;
						Debug.LogWarningFormat(
							"MajorDomo server signalled error: {0}",
							error.Message);
						Cleanup();
					}
				}
				else
				{
					Debug.LogWarningFormat(
						"Could not connect to MajorDomo server at '{0}': Timeout",
						address);
					Cleanup();
				}
			}

			return success;
		}


		public bool IsConnected()
		{
			return m_client != null;
		}


		public uint ClientUID { get { return m_client.ClientUID; } private set { } }


		public bool RetrieveClientList()
		{
			bool success = false;

			if (IsConnected())
			{
				// build request
				m_bufOut.Clear();
				var clientUIDs = AUT_WH.MajorDomoProtocol.ClReq_GetClientList.CreateClientUIDsVector(m_bufOut, new uint[] { });
				var requestGetClientList = AUT_WH.MajorDomoProtocol.ClReq_GetClientList.CreateClReq_GetClientList(m_bufOut, clientUIDs);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_GetClientList, requestGetClientList.Value))
				{
					if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_GetClientList)
					{
						List<ClientData> retrievedClients = new List<ClientData>();

						var repClientList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_GetClientList>().Value;
						String output = "Queried " + repClientList.ClientsLength + " clients:\n";
						for (int i = 0; i < repClientList.ClientsLength; i++)
						{
							var clientInfo = repClientList.Clients(i).Value;
							output += "- " + (i + 1) + ": '" + clientInfo.Name + "', UID " + clientInfo.Uid + "\n";
							ClientData client = new ClientData(clientInfo);
							ClientManager.AddClient(client);
							retrievedClients.Add(client);
						}
						Debug.Log(output);

						success = true;

						// call event handlers
						foreach (ClientData client in retrievedClients)
						{
							OnClientRegistered?.Invoke(client);
						}
					}
				}
			}

			return success;
		}


		public bool RetrieveEntityList()
		{
			bool success = false;

			if (IsConnected())
			{
				// build request
				m_bufOut.Clear();
				var entityUIDs = AUT_WH.MajorDomoProtocol.ClReq_GetEntityList.CreateEntityUIDsVector(m_bufOut, new uint[] { });
				var requestGetEntityList = AUT_WH.MajorDomoProtocol.ClReq_GetEntityList.CreateClReq_GetEntityList(m_bufOut, entityUIDs);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_GetEntityList, requestGetEntityList.Value))
				{
					if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_GetEntityList)
					{
						List<EntityData> retrievedEntites = new List<EntityData>();

						var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_GetEntityList>().Value;
						String output = "Queried " + repEntityList.EntitiesLength + " entities:\n";
						for (int i = 0; i < repEntityList.EntitiesLength; i++)
						{
							var entityInformation = repEntityList.Entities(i).Value;
							EntityData entity = new EntityData(entityInformation);
							output += "- " + (i + 1) + ": " + entity.ToString(true, true) + "\n";
							entity = EntityManager.AddEntity(entity);
							retrievedEntites.Add(entity);
						}

						Debug.Log(output);
						m_entityListRetrieved = true;

						success = true;

						// call event handlers
						OnEntitiesPublished?.Invoke(retrievedEntites);
						foreach (var entity in retrievedEntites)
						{
							entity.InvokeOnUpdatedHandlers();
						}
					}
				}
			}

			return success;
		}


		public EntityData CreateClientEntity(string _name)
		{
			EntityData entity = null;
			if ((m_client != null) && m_entityListRetrieved)
			{
				entity = EntityManager.CreateClientEntity(_name);
			}
			return entity;
		}


		public void PublishEntity(EntityData _entity)
		{
			List<EntityData> entityList = new List<EntityData>();
			entityList.Add(_entity);
			PublishEntities(entityList);
		}


		public void PublishEntities(List<EntityData> _entities)
		{
			if (_entities.Count == 0) return;

			// prepare publish packet
			m_bufOut.Clear();

			var listInfos = new List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityInformation>>();
			foreach (var entity in _entities)
			{
				entity.SetClientUID(m_client.ClientUID);
				listInfos.Add(entity.WriteEntityInformation(m_bufOut));
			}
			var entityInfos = AUT_WH.MajorDomoProtocol.ClReq_PublishEntities.CreateEntitiesVector(m_bufOut, listInfos.ToArray());
			var requestPublishEntities = AUT_WH.MajorDomoProtocol.ClReq_PublishEntities.CreateClReq_PublishEntities(m_bufOut, m_client.ClientUID, entityInfos);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_PublishEntities, requestPublishEntities.Value))
			{
				string dbgOK = "";
				string dbgFail = "";
				List<EntityData> publishedEntities = new List<EntityData>();
				var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_PublishEntities>().Value;
				for (int idx = 0; idx < repEntityList.EntityUIDsLength; idx++)
				{
					uint entityUID = repEntityList.EntityUIDs(idx);
					EntityData entity = _entities[idx];
					if (entityUID > 0)
					{
						entity.SetEntityUID(entityUID);
						entity = EntityManager.AddEntity(entity);
						publishedEntities.Add(entity);
						dbgOK += "- " + entity.ToString(true, true) + "\n";
					}
					else
					{
						dbgFail += "- " + entity.ToString(true, true) + "\n";
					}
				}

				if (dbgOK.Length > 0) Debug.Log("Published entities:\n" + dbgOK);
				if (dbgFail.Length > 0) Debug.LogWarning("Entities that could not be published:\n" + dbgFail);

				// call event handlers
				if (publishedEntities.Count > 0)
				{
					OnEntitiesPublished?.Invoke(publishedEntities);
				}
			}
		}


		public void RevokeEntity(EntityData _entity)
		{
			List<EntityData> entityList = new List<EntityData>();
			entityList.Add(_entity);
			RevokeEntities(entityList);
		}


		public void RevokeEntities(List<EntityData> _entities)
		{
			if (_entities.Count == 0) return;

			// work on copy of list
			List<EntityData> entitiesToRevoke = new List<EntityData>(_entities);
			// don't revoke twice
			entitiesToRevoke.RemoveAll(entity => entity.State == EntityData.EntityState.Revoked);

			// prepare revoke packet
			m_bufOut.Clear();
			m_bufOut.StartVector(sizeof(uint), entitiesToRevoke.Count, sizeof(int));
			foreach (var entity in entitiesToRevoke)
			{
				m_bufOut.AddUint(entity.EntityUID);
			}
			var entityUIDs = m_bufOut.EndVector();
			var requestRevokeEntities = AUT_WH.MajorDomoProtocol.ClReq_RevokeEntities.CreateClReq_RevokeEntities(m_bufOut, m_client.ClientUID, entityUIDs);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_RevokeEntities, requestRevokeEntities.Value))
			{
				string dbgOK = "";
				string dbgFail = "";
				// remove revoked entities from manager
				List<EntityData> revokedEntities = new List<EntityData>();
				var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_RevokeEntities>().Value;
				for (int idx = 0; idx < repEntityList.EntityUIDsLength; idx++)
				{
					// get next revoked entity UID
					uint entityUID = repEntityList.EntityUIDs(idx);
					// find corresponding entity - first by array index
					EntityData entity = null;
					if (idx < entitiesToRevoke.Count)
					{
						entity = entitiesToRevoke[idx];
						// UID match?
						if (entity.EntityUID != entityUID)
						{
							entity = null;
						}
					}
					if (entity == null)
					{
						// not found: brute force search
						foreach (var e in entitiesToRevoke)
						{
							if (e.EntityUID == entityUID)
							{
								entity = e;
								break;
							}
						}
					}

					if (entity != null) 
					{
						EntityManager.RemoveEntity(entity);
						entity.SetRevoked();
						entitiesToRevoke.Remove(entity); //remove from list for later
						revokedEntities.Add(entity);
						dbgOK += "- " + entity.ToString(true, true) + "\n";
					}
				}

				// which entities were not revoked? (left in the list)
				foreach (var entity in entitiesToRevoke)
				{
					dbgFail += "- " + entity.ToString(true, true) + "\n";
				}

				if (dbgOK.Length > 0) Debug.Log("Revoked entities:\n" + dbgOK);
				if (dbgFail.Length > 0) Debug.LogWarning("Entities that could not be revoked:\n" + dbgFail);

				// call event handlers
				if (revokedEntities.Count > 0)
				{
					OnEntitiesRevoked?.Invoke(revokedEntities);
				}
			}
		}


		public void RevokeOwnEntites(bool _includePersistent)
		{
			if (m_client == null) return;

			List<EntityData> entities = EntityManager.GetClientEntities();
			if (!_includePersistent)
			{
				// remove persistent entities
				entities.RemoveAll(entity => entity.IsPersistent());
			}
			RevokeEntities(entities);
		}


		public void RequestEntityControl(EntityData _entity)
		{
			List<EntityData> entityList = new List<EntityData>();
			entityList.Add(_entity);
			RequestEntityControl(entityList);
		}


		public void RequestEntityControl(List<EntityData> _entities)
		{
			if (_entities.Count == 0) return;

			// prepare control request packet
			m_bufOut.Clear();
			m_bufOut.StartVector(sizeof(uint), _entities.Count, sizeof(int));
			foreach (var entity in _entities)
			{
				m_bufOut.AddUint(entity.EntityUID);
			}
			var entityUIDs = m_bufOut.EndVector();
			var requestControlEntities = AUT_WH.MajorDomoProtocol.ClReq_RequestEntityControl.CreateClReq_RequestEntityControl(m_bufOut, m_client.ClientUID, entityUIDs);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_RequestEntityControl, requestControlEntities.Value))
			{
				string dbgOK = "";
				// reassigning client UID for controlled entities
				List<EntityData> controlledEntities = new List<EntityData>();
				var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_RequestEntityControl>().Value;
				for (int idx = 0; idx < repEntityList.EntityUIDsLength; idx++)
				{
					uint entityUID = repEntityList.EntityUIDs(idx);
					if (entityUID > 0)
					{
						EntityData entity = EntityManager.FindEntity(entityUID);
						if (EntityManager.ChangeEntityControl(entity, m_client.ClientUID))
						{
							controlledEntities.Add(entity);
							dbgOK += "- " + entity.ToString(true, true) + "\n";
						}
						_entities.Remove(entity);
					}
				}
				if (dbgOK.Length > 0) Debug.Log("Controlled entities:\n" + dbgOK);

				string dbgFail = "";
				for (int idx = 0; idx < _entities.Count; idx++)
				{
					dbgFail += "- " + _entities[idx].ToString(true, true) + "\n";
				}
				if (dbgFail.Length > 0) Debug.LogWarning("Entities that could not be controlled:\n" + dbgFail);

				// call event handlers
				if (controlledEntities.Count > 0)
				{
					OnEntityControlChanged?.Invoke(controlledEntities);
				}
			}
		}


		public void ReleaseEntityControl(List<EntityData> _entities)
		{
			if (_entities.Count == 0) return;

			// prepare control release packet
			m_bufOut.Clear();
			m_bufOut.StartVector(sizeof(uint), _entities.Count, sizeof(int));
			foreach (var entity in _entities)
			{
				m_bufOut.AddUint(entity.EntityUID);
			}
			var entityUIDs = m_bufOut.EndVector();
			var releaseControlEntities = AUT_WH.MajorDomoProtocol.ClReq_ReleaseEntityControl.CreateClReq_ReleaseEntityControl(m_bufOut, m_client.ClientUID, entityUIDs);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ReleaseEntityControl, releaseControlEntities.Value))
			{
				string dbgOK = "";
				// reassigning client UID for released entities
				List<EntityData> releasedEntities = new List<EntityData>();
				var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_ReleaseEntityControl>().Value;
				for (int idx = 0; idx < repEntityList.EntityUIDsLength; idx++)
				{
					uint entityUID = repEntityList.EntityUIDs(idx);
					if (entityUID > 0)
					{
						EntityData entity = EntityManager.FindEntity(entityUID);
						if (EntityManager.ChangeEntityControl(entity, ClientData.UID_SERVER))
						{
							releasedEntities.Add(entity);
							dbgOK += "- " + entity.ToString(true, true) + "\n";
						}
						_entities.Remove(entity);
					}
				}
				if (dbgOK.Length > 0) Debug.Log("Released entities:\n" + dbgOK);

				string dbgFail = "";
				for (int idx = 0; idx < _entities.Count; idx++)
				{
					dbgFail += "- " + _entities[idx].ToString(true, true) + "\n";
				}
				if (dbgFail.Length > 0) Debug.LogWarning("Entities that could not be released:\n" + dbgFail);

				// call event handlers
				if (releasedEntities.Count > 0)
				{
					OnEntityControlChanged?.Invoke(releasedEntities);
				}
			}
		}


		public void SendBroadcast(uint _identifier, string _message)
		{
			SendBroadcast(_identifier, System.Text.Encoding.UTF8.GetBytes(_message));
		}


		public void SendBroadcast(uint _identifier, byte[] _data)
		{
			// prepare broadcast
			m_bufOut.Clear();
			// add data buffer (reverse order)
			m_bufOut.StartVector(1, _data.Length, 1);
			for (int idx = _data.Length - 1; idx >= 0; idx--)
			{
				m_bufOut.AddByte(_data[idx]);
			}
			var data = m_bufOut.EndVector();
			var broadcast = AUT_WH.MajorDomoProtocol.ClReq_ClientBroadcast.CreateClReq_ClientBroadcast(m_bufOut, m_client.ClientUID, _identifier, data);
			// send and receive
			if (!ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ClientBroadcast, broadcast.Value))
			{
				Debug.LogWarning("Could not send broadcast");
			}
		}


		public void Process()
		{
			ProcessServerEvents();
			ProcessClientUpdates();
			ProcessServerUpdates();
		}


		public void ProcessServerEvents()
		{
			if (!IsConnected()) return;
			if (m_serverEventSocket != null)
			{
				// process as long as there are events (max 5 to avoid deadlock)
				int maxEventsToProcess = 5;
				while (maxEventsToProcess > 0 &&  m_serverEventSocket.Poll(TimeSpan.Zero))
				{
					maxEventsToProcess--;
				}
			}
		}


		public void ProcessClientUpdates()
		{
			if (!IsConnected()) return;
			// are there entities that have been modified
			if (m_clientUpdateSocket != null)
			{
				ProcessModifiedEntities();
			}
		}


		public void ProcessServerUpdates()
		{
			if (!IsConnected()) return;
			// process as long as there are updates
			if (m_serverUpdateSocket != null)
			{
				// process as long as there are events (max 5 to avoid deadlock)
				int maxUpdatesToProcess = 5;
				while (maxUpdatesToProcess > 0 && m_serverUpdateSocket.Poll(TimeSpan.Zero))
				{
					maxUpdatesToProcess--;
				}
			}
		}


		public void Disconnect(bool _revokeOwnEntites = true)
		{
			if (IsConnected())
			{
				if (_revokeOwnEntites)
				{
					RevokeOwnEntites(false);
				}

				Debug.Log("Trying to disconnect from MajorDomo server...");

				// build request
				m_bufOut.Clear();
				var requestClientDisconnect = AUT_WH.MajorDomoProtocol.ClReq_ClientDisconnect.CreateClReq_ClientDisconnect(m_bufOut, m_client.ClientUID);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ClientDisconnect, requestClientDisconnect.Value) && 
					m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_ClientDisconnect)
				{
					Debug.Log("Disconnected from MajorDomo server");
				}

				ClientData client = m_client; // save for event call

				EntityManager.Reset();
				ClientManager.Reset();
				m_client = null; // that's it

				// fake the event of this client disconnecting
				OnClientUnregistered?.Invoke(client);
			}
			Cleanup();
		}


		protected void Cleanup()
		{
			if (m_serverUpdateSocket != null)
			{
				m_serverUpdateSocket.Dispose();
				m_serverUpdateSocket = null;
			}

			if (m_serverEventSocket != null)
			{
				m_serverEventSocket.Dispose();
				m_serverEventSocket = null;
			}

			if (m_clientUpdateSocket != null)
			{
				m_clientUpdateSocket.Dispose();
				m_clientUpdateSocket = null;
			}

			if (m_clientRequestSocket != null)
			{
				m_clientRequestSocket.Dispose();
				m_clientRequestSocket = null;
			}

			NetMQ.NetMQConfig.Cleanup(false);
		}


		public ulong GetTimestamp()
		{
			return (ulong)(DateTime.Now - m_startTime).TotalMilliseconds;
		}


		private bool ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest _requestType, int _requestOffset)
		{
			bool success = false;
			try
			{
				// prepare message to send
				var req = AUT_WH.MajorDomoProtocol.ClientRequest.CreateClientRequest(m_bufOut, GetTimestamp(), _requestType, _requestOffset);
				m_bufOut.Finish(req.Value);

				byte[] buf = m_bufOut.SizedByteArray();
				m_msgOut.InitGC(buf, buf.Length);

				if (m_clientRequestSocket.TrySend(ref m_msgOut, TimeSpan.Zero, false))
				{
					m_msgIn.InitEmpty();
					if (m_clientRequestSocket.TryReceive(ref m_msgIn, TimeSpan.FromSeconds(m_timeoutInterval)) && (m_msgIn.Data != null))
					{
						m_bufIn = new FlatBuffers.ByteBuffer(m_msgIn.Data);
						m_serverReply = AUT_WH.MajorDomoProtocol.ServerReply.GetRootAsServerReply(m_bufIn, m_serverReply);
						if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_Error)
						{
							var error = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_Error>().Value;
							Debug.LogWarningFormat(
								"MajorDomo server signalled error: {0}",
								error.Message);
						}
						else
						{
							success = true;
						}
					}
				}
				else
				{
					Debug.LogWarning("Could not send request");
				}
			}
			catch (Exception e)
			{
				Debug.LogWarningFormat(
					"Exception while sending: {0}", 
					e.Message);
			}

			return success;
		}


		private void ServerEvent_ReceiveReady(object sender, NetMQ.NetMQSocketEventArgs e)
		{
			m_msgIn.InitEmpty();
			if (e.Socket.TryReceive(ref m_msgIn, TimeSpan.Zero) && (m_msgIn.Data != null))
			{
				m_bufIn = new FlatBuffers.ByteBuffer(m_msgIn.Data);
				m_serverEvent = AUT_WH.MajorDomoProtocol.ServerEvent.GetRootAsServerEvent(m_bufIn, m_serverEvent);

				if (!IsConnected()) return; // in case this packet comes too late

				switch (m_serverEvent.EventType)
				{
					case AUT_WH.MajorDomoProtocol.UServerEvent.ServerEvent_ClientConnected:
						{
							var evt = m_serverEvent.Event<AUT_WH.MajorDomoProtocol.ServerEvent_ClientConnected>();
							if (evt.HasValue) ServerEvent_ClientConnected(evt.Value);
							break;
						}

					case AUT_WH.MajorDomoProtocol.UServerEvent.ServerEvent_ClientDisconnected:
						{
							var evt = m_serverEvent.Event<AUT_WH.MajorDomoProtocol.ServerEvent_ClientDisconnected>();
							if (evt.HasValue) ServerEvent_ClientDisconnected(evt.Value);
							break;
						}

					case AUT_WH.MajorDomoProtocol.UServerEvent.ServerEvent_EntitiesPublished:
						{
							var evt = m_serverEvent.Event<AUT_WH.MajorDomoProtocol.ServerEvent_EntitiesPublished>();
							if (evt.HasValue) ServerEvent_EntitiesPublished(evt.Value);
							break;
						}

					case AUT_WH.MajorDomoProtocol.UServerEvent.ServerEvent_EntitiesRevoked:
						{
							var evt = m_serverEvent.Event<AUT_WH.MajorDomoProtocol.ServerEvent_EntitiesRevoked>();
							if (evt.HasValue) ServerEvent_EntitiesRevoked(evt.Value);
							break;
						}

					case AUT_WH.MajorDomoProtocol.UServerEvent.ServerEvent_EntitiesChangedClient:
						{
							var evt = m_serverEvent.Event<AUT_WH.MajorDomoProtocol.ServerEvent_EntitiesChangedClient>();
							if (evt.HasValue) ServerEvent_EntityControlChanged(evt.Value);
							break;
						}

					case AUT_WH.MajorDomoProtocol.UServerEvent.ServerEvent_ClientBroadcast:
						{
							var evt = m_serverEvent.Event<AUT_WH.MajorDomoProtocol.ServerEvent_ClientBroadcast>();
							if (evt.HasValue) ServerEvent_ClientBroadcast(evt.Value);
							break;
						}

					case AUT_WH.MajorDomoProtocol.UServerEvent.ServerEvent_ServerShutdown:
						{
							var evt = m_serverEvent.Event<AUT_WH.MajorDomoProtocol.ServerEvent_ServerShutdown>();
							if (evt.HasValue) ServerEvent_ServerShutdown(evt.Value);
							break;
						}

					default:
						Debug.LogWarningFormat(
							"Unhandled server event {0}",
							m_serverEvent.EventType.ToString());
						break;
				}
			}
		}


		private void ServerEvent_ClientConnected(AUT_WH.MajorDomoProtocol.ServerEvent_ClientConnected _event)
		{
			if (!_event.Client.HasValue) return;

			var clientInformation = _event.Client.Value;
			ClientData client = new ClientData(clientInformation);
			// don't react to this client being announced
			if (client.ClientUID != m_client.ClientUID)
			{
				Debug.LogFormat(
					"Server event: client {0} connected",
					client.ToString());
				ClientManager.AddClient(client);
				OnClientRegistered?.Invoke(client);
			}
		}


		private void ServerEvent_ClientDisconnected(AUT_WH.MajorDomoProtocol.ServerEvent_ClientDisconnected _event)
		{
			var clientUID = _event.ClientUID;
			ClientData client = ClientManager.GetClientByUID(clientUID);
			if (client != null)
			{
				Debug.LogFormat(
					"Server event: client {0} disconnected",
					client.ToString());
				OnClientUnregistered?.Invoke(client);
				ClientManager.RemoveClient(client);
			}
		}


		private void ServerEvent_EntitiesPublished(AUT_WH.MajorDomoProtocol.ServerEvent_EntitiesPublished _event)
		{
			List<EntityData> publishedEntities = new List<EntityData>();
			string dbg = "";
			for (int idx = 0; idx < _event.EntitiesLength; idx++)
			{
				var entityInformation = _event.Entities(idx).Value;
				EntityData entity = new EntityData(entityInformation);
				if (entity.ClientUID != m_client.ClientUID)
				{
					entity = EntityManager.AddEntity(entity);
					publishedEntities.Add(entity);
					if (dbg.Length > 0) dbg += ", ";
					dbg += entity.ToString(false, false);
				}
			}
			if (publishedEntities.Count > 0)
			{
				Debug.Log("Server event: publish entities " + dbg);
				OnEntitiesPublished?.Invoke(publishedEntities);
				foreach (var entity in publishedEntities)
				{
					entity.InvokeOnUpdatedHandlers();
				}
			}
		}


		private void ServerEvent_EntitiesRevoked(AUT_WH.MajorDomoProtocol.ServerEvent_EntitiesRevoked _event)
		{
			List<EntityData> revokedEntities = new List<EntityData>();
			string dbg = "";
			for (int idx = 0; idx < _event.EntityUIDsLength; idx++)
			{
				EntityData entity = EntityManager.FindEntity(_event.EntityUIDs(idx));
				if (entity != null)
				{
					EntityManager.RemoveEntity(entity);
					entity.SetRevoked();
					revokedEntities.Add(entity);
					if (dbg.Length > 0) dbg += ", ";
					dbg += entity.ToString(false, false);
				}
			}
			// were any of the revoked entities from other clients?
			if (revokedEntities.Count > 0)
			{
				Debug.Log("Server event: revoke entities " + dbg);
				OnEntitiesRevoked?.Invoke(revokedEntities);
			}
		}


		private void ServerEvent_EntityControlChanged(AUT_WH.MajorDomoProtocol.ServerEvent_EntitiesChangedClient _event)
		{
			List<EntityData> entities = new List<EntityData>();
			string dbg = "";
			uint newClientUID = _event.ClientUID;
			for (int idx = 0; idx < _event.EntityUIDsLength; idx++)
			{
				EntityData entity = EntityManager.FindEntity(_event.EntityUIDs(idx));
				if (entity != null)
				{
					if (EntityManager.ChangeEntityControl(entity, newClientUID))
					{
						entities.Add(entity);
						if (dbg.Length > 0) dbg += ", ";
						dbg += entity.ToString(false, false);
					}
				}
			}
			if (entities.Count > 0)
			{
				Debug.Log("Server event: change entity control " + dbg);
				OnEntityControlChanged?.Invoke(entities);
			}
		}


		private void ServerEvent_ClientBroadcast(AUT_WH.MajorDomoProtocol.ServerEvent_ClientBroadcast _event)
		{
			var clientUID = _event.ClientUID;
			ClientData client = ClientManager.GetClientByUID(clientUID);
			// is client valid and not myself?
			if ((client != null) && (client.ClientUID != m_client.ClientUID))
			{
				Debug.LogFormat(
					"Server event: broadcast from client {0}",
					client.ToString());
				Broadcast broadcast = new Broadcast(client, _event);
				OnClientBroadcastReceived?.Invoke(broadcast);
			}
		}


		private void ServerEvent_ServerShutdown(AUT_WH.MajorDomoProtocol.ServerEvent_ServerShutdown _event)
		{
			Debug.LogFormat(
				"Server event: {0}",
				(_event.Rebooting ? "rebooting" : "shutdown"));
			OnServerShutdown?.Invoke(_event.Rebooting);
		}


		private void ProcessModifiedEntities()
		{
			// find client entities 
			List<EntityData> modifiedEntities = EntityManager.GetModifiedEntities();
		
			// send a packet regularly as heartbeat, even if empty
			bool sendHeartbeat = (DateTime.Now - m_lastUpdateSent).TotalSeconds > m_heartbeatInterval;

			// is there anything to send at all?
			if (!sendHeartbeat && modifiedEntities.Count == 0) return;

			// prepare update packet
			//string dbg = "";
			m_bufOut.Clear();
			var listUpdates = new List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityUpdate>>();
			foreach (var entity in modifiedEntities)
			{
				listUpdates.Add(entity.WriteEntityUpdate(m_bufOut));
			}

			//Debug.Log("Sending update for entities " + dbg);
			var entityInfos = AUT_WH.MajorDomoProtocol.EntityUpdates.CreateUpdatesVector(m_bufOut, listUpdates.ToArray());
			ulong timestamp = GetTimestamp();

			var entityUpdates = AUT_WH.MajorDomoProtocol.EntityUpdates.CreateEntityUpdates(m_bufOut, timestamp, m_client.ClientUID, entityInfos);
			m_bufOut.Finish(entityUpdates.Value);
		
			// send update packet
			byte[] buf = m_bufOut.SizedByteArray();
			m_msgOut.InitGC(buf, buf.Length);
			m_clientUpdateSocket.TrySend(ref m_msgOut, TimeSpan.Zero, false);
			// remember last update time
			m_lastUpdateSent = DateTime.Now;

			foreach (var entity in modifiedEntities)
			{
				entity.InvokeOnModifiedHandlers();
			}

			EntityManager.ResetModifiedEntities();
		}


		private void ServerUpdate_ReceiveReady(object sender, NetMQ.NetMQSocketEventArgs e)
		{
			m_msgIn.InitEmpty();
			if (e.Socket.TryReceive(ref m_msgIn, TimeSpan.Zero) && (m_msgIn.Data != null))
			{
				m_bufIn = new FlatBuffers.ByteBuffer(m_msgIn.Data);
				m_entityUpdates = AUT_WH.MajorDomoProtocol.EntityUpdates.GetRootAsEntityUpdates(m_bufIn, m_entityUpdates);

				if (!IsConnected()) return; // in case this packet comes too late

				// only apply updates from other clients
				if (m_entityUpdates.ClientUID != m_client.ClientUID)
				{
					// read all updates first, then call handlers
					m_updatedEntities.Clear();

					// Debug.Log("cUID:" + m_entityUpdates.ClientUID + " " + m_entityUpdates.UpdatesLength + " " + m_entityUpdates.Timestamp);
					for (int idxUpdate = 0; idxUpdate < m_entityUpdates.UpdatesLength; idxUpdate++)
					{
						var update = m_entityUpdates.Updates(idxUpdate);
						if (update.HasValue)
						{
							EntityData updatedEntity = EntityManager.UpdateEntity(update.Value);
							if (updatedEntity != null)
							{
								m_updatedEntities.Add(updatedEntity);
							}
						}
					}

					foreach (var entity in m_updatedEntities)
					{
						entity.InvokeOnUpdatedHandlers();
					}
				}
			}
		}


		private readonly DateTime   m_startTime;
		private          ClientData m_client;

		private NetMQ.Sockets.RequestSocket    m_clientRequestSocket;
		private NetMQ.Sockets.SubscriberSocket m_serverEventSocket;
		private NetMQ.Sockets.PushSocket       m_clientUpdateSocket;
		private NetMQ.Sockets.SubscriberSocket m_serverUpdateSocket;

		private float    m_timeoutInterval;
		private float    m_heartbeatInterval;
		private DateTime m_lastUpdateSent;

		private FlatBuffers.FlatBufferBuilder  m_bufOut;
		private NetMQ.Msg                      m_msgOut;
		private FlatBuffers.ByteBuffer         m_bufIn;
		private NetMQ.Msg                      m_msgIn;

		private AUT_WH.MajorDomoProtocol.ServerReply   m_serverReply;
		private AUT_WH.MajorDomoProtocol.EntityUpdates m_entityUpdates;
		private AUT_WH.MajorDomoProtocol.ServerEvent   m_serverEvent;

		private readonly List<EntityData> m_updatedEntities;

		private bool m_entityListRetrieved;
	}
}