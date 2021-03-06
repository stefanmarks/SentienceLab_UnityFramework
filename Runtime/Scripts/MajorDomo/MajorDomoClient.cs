﻿#region Copyright Information
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
		public const int VERSION_MINOR    = 5;
		public const int VERSION_REVISION = 2;


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


		public bool DebugUpdatePackages = false;


		public MajorDomoClient()
		{
			if (!m_NetMQ_Initialised)
			{
				// Necessary to force .NET implementation of network functions
				// Client will most likely hang without this line!!!!!
				AsyncIO.ForceDotNet.Force();
				m_NetMQ_Initialised = true;
			}

			ClientManager = new ClientManager();
			EntityManager = new EntityManager();

			m_startTime = DateTime.Now;

			m_client = null;

			m_bufReq    = new FlatBuffers.FlatBufferBuilder(1024);
			m_msgReq    = new NetMQ.Msg();
			m_bufUpdate = new FlatBuffers.FlatBufferBuilder(1024);
			m_msgUpdate = new NetMQ.Msg();
			m_msgIn     = new NetMQ.Msg();
			m_serverReply   = new AUT_WH.MajorDomoProtocol.ServerReply();
			m_serverEvent   = new AUT_WH.MajorDomoProtocol.ServerEvent();
			m_entityUpdates = new AUT_WH.MajorDomoProtocol.EntityUpdates();

			m_clientRequestSocket = null;
			m_serverEventSocket   = null;
			m_clientUpdateSocket  = null;
			m_serverUpdateSocket  = null;

			m_updatedEntities = new List<EntityData>();

			m_lastUpdateSent = DateTime.Now;

			m_localClientCount++;
		}


		~MajorDomoClient()
		{
			// one client less. are we done and have to clean up the NetMQ context?
			if (m_localClientCount > 0)
			{
				m_localClientCount--;
			}
			else
			{
				Terminate();
			}
		}


		public static void Terminate()
		{
			if (m_NetMQ_Initialised)
			{
				NetMQ.NetMQConfig.Cleanup(false);
				m_NetMQ_Initialised = false;
			}
		}


		public bool Connect(string _applicationName, string _serverAddress, ushort _serverPort, float _timeout)
		{
			bool success = false;

			if (!IsConnected())
			{
				string address = "tcp://" + _serverAddress + ":" + _serverPort;
				Debug.LogFormat(
					"'{0}' trying to connect to MajorDomo server at '{1}'...", 
					_applicationName, address);
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
						"'{0}' could not connect to MajorDomo server at '{1}': {2}",
						_applicationName, address, e.Message);
					Cleanup();
					return false;
				}

				Debug.LogFormat(
					"'{0}' sending connection request...",
					_applicationName);

				// build request
				m_bufReq.Clear();
				var clientName = m_bufReq.CreateString(_applicationName);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.StartClReq_ClientConnect(m_bufReq);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.AddClientName(m_bufReq, clientName);
				var clientVersion = AUT_WH.MajorDomoProtocol.Version.CreateVersion(m_bufReq,
					VERSION_MAJOR,
					VERSION_MINOR,
					VERSION_REVISION);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.AddClientVersion(m_bufReq, clientVersion);
				var clientProtocol = AUT_WH.MajorDomoProtocol.Version.CreateVersion(m_bufReq,
					(sbyte)AUT_WH.MajorDomoProtocol.EProtocolVersion.MAJOR,
					(sbyte)AUT_WH.MajorDomoProtocol.EProtocolVersion.MINOR,
					(ushort)AUT_WH.MajorDomoProtocol.EProtocolVersion.REVISION);
				AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.AddClientProtocol(m_bufReq, clientProtocol);
				var reqClientConnect = AUT_WH.MajorDomoProtocol.ClReq_ClientConnect.EndClReq_ClientConnect(m_bufReq);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ClientConnect, reqClientConnect.Value))
				{
					if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_ClientConnect)
					{
						var ack = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_ClientConnect>().Value;
						Debug.LogFormat(
							"'{0}' connected to MajorDomo server '{1}' v{2}.{3}.{4} (protocol v{5}.{6}.{7}, server time {8}) with Client UID {9}",
							_applicationName, ack.ServerName,
							ack.ServerVersion.Value.NumMajor, ack.ServerVersion.Value.NumMinor, ack.ServerVersion.Value.NumRevision,
							ack.ServerProtocol.Value.NumMajor, ack.ServerProtocol.Value.NumMinor, ack.ServerProtocol.Value.NumRevision,
							m_serverReply.Timestamp,
							ack.ClientUID);

						m_client = new ClientData(_applicationName, ack.ClientUID);
						
						// TODO: Catch any exceptions happening during the following three connections

						// create other connections for:
						// server events
						string serverEventSocket = "tcp://" + _serverAddress + ":" + ack.ServerEventPort;
						Debug.LogFormat("Server event socket: {0}", serverEventSocket);
						m_serverEventSocket = new NetMQ.Sockets.SubscriberSocket(serverEventSocket);
						m_serverEventSocket.SubscribeToAnyTopic();
						
						// Client updates
						string clientUpdateSocket = "tcp://" + _serverAddress + ":" + ack.ClientUpdatePort;
						Debug.LogFormat("Client update socket: {0}", clientUpdateSocket);
						m_clientUpdateSocket = new NetMQ.Sockets.PushSocket(clientUpdateSocket);
					
						// Server updates
						string serverUpdateSocket = "tcp://" + _serverAddress + ":" + ack.ServerUpdatePort;
						Debug.LogFormat("Server update socket: {0}", serverUpdateSocket);
						m_serverUpdateSocket = new NetMQ.Sockets.SubscriberSocket(serverUpdateSocket);
						m_serverUpdateSocket.SubscribeToAnyTopic();
						
						// get interval for sending heartbeat signal
						m_heartbeatInterval = ack.HeartbeatInterval / 1000.0f;
						// get server properties
						m_serverAllowsRemoteControl =
							(ack.ServerProperties & AUT_WH.MajorDomoProtocol.ServerProperties.RemoteControlAllowed) > 0;

						success = true;

						m_entityListRetrieved = false;
						ClientManager.Reset();
						RetrieveClientList();
						
						EntityManager.Reset();
						EntityManager.SetClientUID(this.ClientUID);
						RetrieveEntityList();

						// now that we have the lists, we can also start receiving events
						// (not beforehand, otherwise event data might mix with query data)
						m_serverEventSocket.ReceiveReady  += ServerEvent_ReceiveReady;
						m_serverUpdateSocket.ReceiveReady += ServerUpdate_ReceiveReady;

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
			else
			{
				Debug.LogWarning("Already connected to a server");
				success = true;
			}

			return success;
		}


		public bool IsConnected()
		{
			return m_client != null;
		}


		public uint ClientUID 
		{ 
			get 
			{ 
				return m_client != null ? m_client.ClientUID : ClientData.UID_UNASSIGNED; 
			}
			private set { } 
		}


		public bool RetrieveClientList()
		{
			bool success = false;

			if (IsConnected())
			{
				// build request
				m_bufReq.Clear();
				var clientUIDs = AUT_WH.MajorDomoProtocol.ClReq_GetClientList.CreateClientUIDsVector(m_bufReq, new uint[] { });
				var requestGetClientList = AUT_WH.MajorDomoProtocol.ClReq_GetClientList.CreateClReq_GetClientList(m_bufReq, clientUIDs);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_GetClientList, requestGetClientList.Value))
				{
					if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_GetClientList)
					{
						List<ClientData> retrievedClients = new List<ClientData>();

						var repClientList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_GetClientList>().Value;
						for (int i = 0; i < repClientList.ClientsLength; i++)
						{
							var clientInfo = repClientList.Clients(i).Value;
							ClientData client = new ClientData(clientInfo);
							ClientManager.AddClient(client);
							retrievedClients.Add(client);
						}
						Debug.LogFormat("Queried " + retrievedClients.Count + " clients:\n{0}",
							ClientManager.ClientListAsString(retrievedClients));

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
				m_bufReq.Clear();
				var entityUIDs = AUT_WH.MajorDomoProtocol.ClReq_GetEntityList.CreateEntityUIDsVector(m_bufReq, new uint[] { });
				var requestGetEntityList = AUT_WH.MajorDomoProtocol.ClReq_GetEntityList.CreateClReq_GetEntityList(m_bufReq, entityUIDs);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_GetEntityList, requestGetEntityList.Value))
				{
					if (m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_GetEntityList)
					{
						List<EntityData> retrievedEntites = new List<EntityData>();

						var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_GetEntityList>().Value;
						for (int i = 0; i < repEntityList.EntitiesLength; i++)
						{
							var entityInformation = repEntityList.Entities(i).Value;
							EntityData entity = new EntityData(entityInformation);
							entity = EntityManager.AddEntity(entity);
							retrievedEntites.Add(entity);
						}

						Debug.LogFormat("Queried " + retrievedEntites.Count+ " entities:\n{0}",
							EntityManager.EntityListAsString(retrievedEntites));

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
			m_bufReq.Clear();

			var listInfos = new List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityInformation>>();
			foreach (var entity in _entities)
			{
				entity.SetClientUID(this.ClientUID);
				listInfos.Add(entity.WriteEntityInformation(m_bufReq));
			}
			var entityInfos = AUT_WH.MajorDomoProtocol.ClReq_PublishEntities.CreateEntitiesVector(m_bufReq, listInfos.ToArray());
			var requestPublishEntities = AUT_WH.MajorDomoProtocol.ClReq_PublishEntities.CreateClReq_PublishEntities(m_bufReq, this.ClientUID, entityInfos);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_PublishEntities, requestPublishEntities.Value))
			{
				List<EntityData> publishedEntities = new List<EntityData>();
				List<EntityData> failedEntities    = new List<EntityData>();
				var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_PublishEntities>().Value;

				int repListLen = repEntityList.EntityUIDsLength;
				if (repListLen > _entities.Count)
				{
					// something went horribly wrong
					Debug.LogWarning("Response entity list is larger - truncating");
					// damage control
					repListLen = _entities.Count;
				}

				for (int idx = 0; idx < repListLen; idx++)
				{
					uint entityUID = repEntityList.EntityUIDs(idx);
					EntityData entity = _entities[idx];
					if (entityUID > 0)
					{
						entity.SetEntityUID(entityUID);
						entity = EntityManager.AddEntity(entity);
						publishedEntities.Add(entity);
					}
					else 
					{
						failedEntities.Add(entity);
					}
				}

				if (publishedEntities.Count > 0)
				{
					Debug.LogFormat("Published entities:\n{0}",
						EntityManager.EntityListAsString(publishedEntities));
				}
				if (failedEntities.Count > 0)
				{
					Debug.LogWarningFormat("Entities that could not be published:\n{0}",
						EntityManager.EntityListAsString(failedEntities));
				}

				// call event handlers
				if (publishedEntities.Count > 0)
				{
					OnEntitiesPublished?.Invoke(publishedEntities);
				}
			}
		}


		public void RevokeEntity(EntityData _entity)
		{
			RevokeEntities(new List<EntityData>() { _entity });
		}


		public void RevokeEntities(List<EntityData> _entities)
		{
			if (_entities.Count == 0) return;

			// work on copy of list
			List<EntityData> entitiesToRevoke = new List<EntityData>(_entities);
			// don't revoke twice
			entitiesToRevoke.RemoveAll(entity => entity.State == EntityData.EntityState.Revoked);

			// prepare revoke packet
			m_bufReq.Clear();
			m_bufReq.StartVector(sizeof(uint), entitiesToRevoke.Count, sizeof(int));
			foreach (var entity in entitiesToRevoke)
			{
				m_bufReq.AddUint(entity.EntityUID);
			}
			var entityUIDs = m_bufReq.EndVector();
			var requestRevokeEntities = AUT_WH.MajorDomoProtocol.ClReq_RevokeEntities.CreateClReq_RevokeEntities(m_bufReq, this.ClientUID, entityUIDs);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_RevokeEntities, requestRevokeEntities.Value))
			{
				// remove revoked entities from manager
				List<EntityData> revokedEntities = new List<EntityData>();
				List<EntityData> failedEntities  = new List<EntityData>();
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
					}
				}

				// which entities were not revoked? (left in the list)
				if (revokedEntities.Count > 0)
				{
					Debug.LogFormat("Revoked entities:\n{0}",
						EntityManager.EntityListAsString(revokedEntities));
				}
				if (entitiesToRevoke.Count > 0)
				{
					Debug.LogWarningFormat("Entities that could not be revoked:\n{0}",
						EntityManager.EntityListAsString(entitiesToRevoke));
				}

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
			RequestEntityControl(new List<EntityData>{ _entity });
		}


		public void RequestEntityControl(List<EntityData> _entities)
		{
			if (_entities.Count == 0) return;

			// prepare control request packet
			m_bufReq.Clear();
			m_bufReq.StartVector(sizeof(uint), _entities.Count, sizeof(int));
			foreach (var entity in _entities)
			{
				m_bufReq.AddUint(entity.EntityUID);
			}
			var entityUIDs = m_bufReq.EndVector();
			var requestControlEntities = AUT_WH.MajorDomoProtocol.ClReq_RequestEntityControl.CreateClReq_RequestEntityControl(m_bufReq, this.ClientUID, entityUIDs);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_RequestEntityControl, requestControlEntities.Value))
			{
				// reassigning client UID for controlled entities
				List<EntityData> controlledEntities = new List<EntityData>();
				var repEntityList = m_serverReply.Rep<AUT_WH.MajorDomoProtocol.SvRep_RequestEntityControl>().Value;
				for (int idx = 0; idx < repEntityList.EntityUIDsLength; idx++)
				{
					uint entityUID = repEntityList.EntityUIDs(idx);
					if (entityUID > 0)
					{
						EntityData entity = EntityManager.FindEntity(entityUID);
						if (EntityManager.ChangeEntityControl(entity, this.ClientUID))
						{
							controlledEntities.Add(entity);
						}
						_entities.Remove(entity);
					}
				}

				if (controlledEntities.Count > 0)
				{
					Debug.LogFormat("Controlled entities:\n{0}",
						EntityManager.EntityListAsString(controlledEntities));
				}
				if (_entities.Count > 0)
				{
					Debug.LogWarningFormat("Entities that could not be controlled:\n{0}",
						EntityManager.EntityListAsString(_entities));
				}

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
			m_bufReq.Clear();
			m_bufReq.StartVector(sizeof(uint), _entities.Count, sizeof(int));
			foreach (var entity in _entities)
			{
				m_bufReq.AddUint(entity.EntityUID);
			}
			var entityUIDs = m_bufReq.EndVector();
			var releaseControlEntities = AUT_WH.MajorDomoProtocol.ClReq_ReleaseEntityControl.CreateClReq_ReleaseEntityControl(m_bufReq, this.ClientUID, entityUIDs);
			// send and receive
			if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ReleaseEntityControl, releaseControlEntities.Value))
			{
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
						}
						_entities.Remove(entity);
					}
				}

				if (releasedEntities.Count > 0)
				{
					Debug.LogFormat("Released entities:\n{0}",
						EntityManager.EntityListAsString(releasedEntities));
				}
				if (_entities.Count > 0)
				{
					Debug.LogWarningFormat("Entities that could not be released:\n{0}", 
						EntityManager.EntityListAsString(_entities));
				}

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
			m_bufReq.Clear();
			// add data buffer (reverse order)
			m_bufReq.StartVector(1, _data.Length, 1);
			for (int idx = _data.Length - 1; idx >= 0; idx--)
			{
				m_bufReq.AddByte(_data[idx]);
			}
			var data = m_bufReq.EndVector();
			var broadcast = AUT_WH.MajorDomoProtocol.ClReq_ClientBroadcast.CreateClReq_ClientBroadcast(m_bufReq, this.ClientUID, _identifier, data);
			// send and receive
			if (!ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ClientBroadcast, broadcast.Value))
			{
				Debug.LogWarning("Could not send broadcast");
			}
		}


		public bool CanRemoteControlServer()
		{
			return IsConnected() && m_serverAllowsRemoteControl;
		}


		public void ServerControl_stopServer(bool _restart, bool _purgePersistentEntities)
		{
			if (!CanRemoteControlServer()) return;

			string strAction = _restart ? "restart server" : "stop server";
			Debug.LogFormat("Sending remote control request: {0}", strAction);
			// build request
			m_bufReq.Clear();
			var commandStop = AUT_WH.MajorDomoProtocol.RemoteControlCommand_StopServer.CreateRemoteControlCommand_StopServer(m_bufReq,
				_restart, _purgePersistentEntities);
			var command = AUT_WH.MajorDomoProtocol.ClReq_RemoteControlCommand.CreateClReq_RemoteControlCommand(m_bufReq,
				this.ClientUID,
				AUT_WH.MajorDomoProtocol.URemoteControlCommand.RemoteControlCommand_StopServer,
				commandStop.Value);
			// send and receive
			if (!ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_RemoteControlCommand, command.Value))
			{
				Debug.LogWarning("Could not send broadcast");
			}
		}


		/// <summary>
		/// Processes sending/handling of events and updates.
		/// </summary>
		/// <returns>/c true if at least one protocol packet was handled</returns>
		public bool Process()
		{
			bool didProcess = false;
			didProcess |= ProcessServerEvents();
			didProcess |= ProcessClientUpdates();
			didProcess |= ProcessServerUpdates();
			return didProcess;
		}


		public bool ProcessServerEvents()
		{
			bool didProcess = false;

			if ((m_serverEventSocket != null) && IsConnected())
			{
				// process as long as there are events (max 5 to avoid deadlock)
				int maxEventsToProcess = 5;
				while (maxEventsToProcess > 0 &&  m_serverEventSocket.Poll(TimeSpan.Zero))
				{
					maxEventsToProcess--;
					didProcess = true;
				}
			}
			return didProcess;
		}


		public bool ProcessClientUpdates()
		{
			bool didProcess = false;
			
			// are there entities that have been modified
			if ((m_clientUpdateSocket != null) && IsConnected())
			{
				didProcess = ProcessModifiedEntities();
			}

			return didProcess;
		}


		public bool ProcessServerUpdates()
		{
			bool didProcess = false;
			
			// process as long as there are updates
			if ((m_serverUpdateSocket != null) && IsConnected())
			{
				// process as long as there are events (max 5 to avoid deadlock)
				int maxUpdatesToProcess = 5;
				while (maxUpdatesToProcess > 0 && m_serverUpdateSocket.Poll(TimeSpan.Zero))
				{
					maxUpdatesToProcess--;
					didProcess = true;
				}
			}
			return didProcess;
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
				m_bufReq.Clear();
				var requestClientDisconnect = AUT_WH.MajorDomoProtocol.ClReq_ClientDisconnect.CreateClReq_ClientDisconnect(m_bufReq, this.ClientUID);
				// send and receive
				if (ProcessClientRequest(AUT_WH.MajorDomoProtocol.UClientRequest.ClReq_ClientDisconnect, requestClientDisconnect.Value) && 
					m_serverReply.RepType == AUT_WH.MajorDomoProtocol.UServerReply.SvRep_ClientDisconnect)
				{
					Debug.Log("Disconnected from MajorDomo server");
				}

				// fake the event of this client disconnecting
				OnClientUnregistered?.Invoke(m_client);

				m_client = null; // that's it
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
				var req = AUT_WH.MajorDomoProtocol.ClientRequest.CreateClientRequest(m_bufReq, GetTimestamp(), _requestType, _requestOffset);
				m_bufReq.Finish(req.Value);

				byte[] buf = m_bufReq.SizedByteArray();
				m_msgReq.InitGC(buf, buf.Length);

				if (m_clientRequestSocket.TrySend(ref m_msgReq, TimeSpan.Zero, false))
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
			if (client.ClientUID != this.ClientUID)
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
			for (int idx = 0; idx < _event.EntitiesLength; idx++)
			{
				var entityInformation = _event.Entities(idx).Value;
				EntityData entity = new EntityData(entityInformation);
				if (entity.ClientUID != this.ClientUID)
				{
					entity = EntityManager.AddEntity(entity);
					publishedEntities.Add(entity);
				}
			}
			if (publishedEntities.Count > 0)
			{
				Debug.LogFormat("Server event: publish entities\n{0}",
					EntityManager.EntityListAsString(publishedEntities));
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
			for (int idx = 0; idx < _event.EntityUIDsLength; idx++)
			{
				EntityData entity = EntityManager.FindEntity(_event.EntityUIDs(idx));
				if (entity != null)
				{
					EntityManager.RemoveEntity(entity);
					entity.SetRevoked();
					revokedEntities.Add(entity);
				}
			}
			// were any of the revoked entities from other clients?
			if (revokedEntities.Count > 0)
			{
				Debug.LogFormat("Server event: revoke entities\n{0}",
					EntityManager.EntityListAsString(revokedEntities));
				OnEntitiesRevoked?.Invoke(revokedEntities);
			}
		}


		private void ServerEvent_EntityControlChanged(AUT_WH.MajorDomoProtocol.ServerEvent_EntitiesChangedClient _event)
		{
			List<EntityData> entities = new List<EntityData>();
			
			uint newClientUID = _event.ClientUID;
			for (int idx = 0; idx < _event.EntityUIDsLength; idx++)
			{
				EntityData entity = EntityManager.FindEntity(_event.EntityUIDs(idx));
				if (entity != null)
				{
					if (EntityManager.ChangeEntityControl(entity, newClientUID))
					{
						entities.Add(entity);
					}
				}
			}
			if (entities.Count > 0)
			{
				Debug.LogFormat("Server event: change entity control\n{0}",
					EntityManager.EntityListAsString(entities));
				OnEntityControlChanged?.Invoke(entities);
			}
		}


		private void ServerEvent_ClientBroadcast(AUT_WH.MajorDomoProtocol.ServerEvent_ClientBroadcast _event)
		{
			var clientUID = _event.ClientUID;
			ClientData client = ClientManager.GetClientByUID(clientUID);
			// is client valid and not myself?
			if ((client != null) && (client.ClientUID != this.ClientUID))
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


		private bool ProcessModifiedEntities()
		{
			// find client entities 
			List<EntityData> modifiedEntities = EntityManager.GetModifiedEntities();

			// send a packet regularly as heartbeat, even if empty
			bool sendHeartbeat = (DateTime.Now - m_lastUpdateSent).TotalSeconds > m_heartbeatInterval;

			// is there anything to send at all?
			if (!sendHeartbeat && modifiedEntities.Count == 0) return false;

			if (DebugUpdatePackages)
			{
				Debug.LogFormat("Sending modified entities:\n{0}",
					EntityManager.EntityListAsString(modifiedEntities));
			}

			// prepare update packet
			m_bufUpdate.Clear();
			var listUpdates = new List<FlatBuffers.Offset<AUT_WH.MajorDomoProtocol.EntityUpdate>>();
			foreach (var entity in modifiedEntities)
			{
				listUpdates.Add(entity.WriteEntityUpdate(m_bufUpdate));
			}

			// Debug.Log("Sending update for entities " + dbg);
			var entityInfos = AUT_WH.MajorDomoProtocol.EntityUpdates.CreateUpdatesVector(m_bufUpdate, listUpdates.ToArray());
			ulong timestamp = GetTimestamp();

			var entityUpdates = AUT_WH.MajorDomoProtocol.EntityUpdates.CreateEntityUpdates(m_bufUpdate, timestamp, this.ClientUID, entityInfos);
			m_bufUpdate.Finish(entityUpdates.Value);
		
			// send update packet
			byte[] buf = m_bufUpdate.SizedByteArray();
			m_msgUpdate.InitGC(buf, buf.Length);
			m_clientUpdateSocket.TrySend(ref m_msgUpdate, TimeSpan.Zero, false);
			// remember last update time
			m_lastUpdateSent = DateTime.Now;

			foreach (var entity in modifiedEntities)
			{
				entity.InvokeOnModifiedHandlers();
			}

			EntityManager.ResetModifiedEntities();
			return true;
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
				if (m_entityUpdates.ClientUID != this.ClientUID)
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

					if (m_updatedEntities.Count > 0)
					{
						if (DebugUpdatePackages)
						{
							Debug.LogFormat("Received entity updates:\n{0}",
							EntityManager.EntityListAsString(m_updatedEntities));
						}

						foreach (var entity in m_updatedEntities)
						{
							entity.InvokeOnUpdatedHandlers();
						}
					}
				}
			}
		}

		private static int  m_localClientCount  = 0;
		private static bool m_NetMQ_Initialised = false;

		private readonly DateTime   m_startTime;
		private          ClientData m_client;

		private NetMQ.Sockets.RequestSocket    m_clientRequestSocket;
		private NetMQ.Sockets.SubscriberSocket m_serverEventSocket;
		private NetMQ.Sockets.PushSocket       m_clientUpdateSocket;
		private NetMQ.Sockets.SubscriberSocket m_serverUpdateSocket;

		private float    m_timeoutInterval;
		private float    m_heartbeatInterval;
		private DateTime m_lastUpdateSent;

		private bool     m_serverAllowsRemoteControl;

		private FlatBuffers.FlatBufferBuilder  m_bufReq;
		private NetMQ.Msg                      m_msgReq;
		private FlatBuffers.FlatBufferBuilder  m_bufUpdate;
		private NetMQ.Msg                      m_msgUpdate;
		private FlatBuffers.ByteBuffer         m_bufIn;
		private NetMQ.Msg                      m_msgIn;

		private AUT_WH.MajorDomoProtocol.ServerReply   m_serverReply;
		private AUT_WH.MajorDomoProtocol.EntityUpdates m_entityUpdates;
		private AUT_WH.MajorDomoProtocol.ServerEvent   m_serverEvent;

		private readonly List<EntityData> m_updatedEntities;

		private bool m_entityListRetrieved;
	}
}