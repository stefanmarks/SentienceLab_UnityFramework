#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;
using System.Collections;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SentienceLab.MajorDomo
{
	[DisallowMultipleComponent]
	[AddComponentMenu("MajorDomo/Manager")]
	public class MajorDomoManager : MonoBehaviour
	{
		[Tooltip("Name of the client to register with the server.\n" +
			"Special strings:\n" +
			"  {IPv4}   : IPv4 Address of the client\n" +
			"  {IPv6}   : IPv6 Address of the client\n" +
			"  {HOST}   : Hostname of the client\n" +
			"  {MACHINE}: Machine name of the client\n" +
			"  {USER}   : User name\n" +
			"  {SCENE}  : Scene name"
		)]

		public string ClientName = "{SCENE}_{HOST}";

		[System.Serializable]
		public class Configuration : ConfigFileBase
		{
			public Configuration() : base("MajorDomoConfig.txt", "MajorDomo") { }

			[System.Serializable]
			public struct ServerInfo
			{
				public string address;
				public ushort port;
				public float  connectionTimeout;
			}

			[Tooltip("List of MajorDomo servers/ports to query and their timeout values in seconds")]
			public List<ServerInfo> Servers = new List<ServerInfo>();

			[Tooltip("Automatic connect delay after startup in seconds (0: Don't Autoconnect)")]
			public float AutoConnectDelay = 0.1f;

			[Tooltip("Automatic disconnect delay after receiving server shutdown event in seconds")]
			public float AutoDisconnectDelay = 0.1f;

			[Tooltip("Automatic reconnect delay after server reboot")]
			public float AutoReconnectDelay = 5.0f;
		}

		[ContextMenuItem("Load configuration from config file", "LoadConfiguration")]
		[ContextMenuItem("Save configuration to config file", "SaveConfiguration")]
		public Configuration configuration = new Configuration();


		/// <summary>
		/// Interface for objects that need to automatically register with the manager on startup
		/// </summary>
		public interface IAutoRegister
		{
			void RegisterWithMajorDomoManager();
		}


		// Singleton accessor
		public static MajorDomoManager Instance
		{
			get
			{
				if (ms_instance == null)
				{
					ms_instance = FindObjectOfType<MajorDomoManager>();
				}
				return ms_instance;
			}
		}


		public event MajorDomoClient.ClientRegistered   OnClientRegistered;
		public event MajorDomoClient.ClientUnregistered OnClientUnregistered;
		public event MajorDomoClient.EntitiesPublished  OnEntitiesPublished;
		public event MajorDomoClient.EntitiesRevoked    OnEntitiesRevoked;

		public event MajorDomoClient.EntityControlChanged OnEntityControlChanged;

		public event MajorDomoClient.ClientBroadcastReceived OnClientBroadcastReceived;

		public event MajorDomoClient.ServerShutdown OnServerShutdown;

		public delegate void ClientConnected();
		public event ClientConnected OnClientConnected;

		public delegate void ClientDisconnecting();
		public event ClientDisconnecting OnClientDisconnecting;


		public EntityData FindEntity(string _entityName)
		{
			return m_client?.EntityManager.FindEntity(_entityName);
		}

		public EntityData CreateClientEntity(string _entityName)
		{
			return m_client?.CreateClientEntity(_entityName);
		}

		public void PublishEntity(EntityData _entity)
		{
			lock (m_entitiesToPublish) { if (!m_entitiesToPublish.Contains(_entity)) m_entitiesToPublish.Add(_entity); }
		}

		public void RevokeEntity(EntityData _entity)
		{
			lock (m_entitiesToRevoke) { if (!m_entitiesToRevoke.Contains(_entity)) m_entitiesToRevoke.Add(_entity); }
		}

		public void RequestControl(EntityData _entity)
		{
			lock (m_entitiesToControl) { if (!m_entitiesToControl.Contains(_entity)) m_entitiesToControl.Add(_entity); }
		}

		public void ReleaseControl(EntityData _entity)
		{
			lock (m_entitiesToRelease) { if (!m_entitiesToRelease.Contains(_entity)) m_entitiesToRelease.Add(_entity); }
		}

		public bool IsConnected()
		{
			return m_state == ManagerState.Connected;
		}

		public uint ClientUID { get { return m_client.ClientUID; } private set { } }

		public void Awake()
		{
			configuration.LoadConfiguration();

			// force instantiation
			m_client = new MajorDomoClient();

			m_entitiesToPublish = new List<EntityData>();
			m_entitiesToRevoke  = new List<EntityData>();
			m_entitiesToControl = new List<EntityData>();
			m_entitiesToRelease = new List<EntityData>();

			m_registeredClients   = new List<ClientData>();
			m_unregisteredClients = new List<ClientData>();
			m_processingClients   = new List<ClientData>();

			m_publishedEntities  = new List<EntityData>();
			m_revokedEntities    = new List<EntityData>();
			m_controlledEntities = new List<EntityData>();
			m_processingEntities = new List<EntityData>();

			m_broadcasts           = new List<Broadcast>();
			m_processingBroadcasts = new List<Broadcast>();

			m_serverShutdown = new List<bool>();

			m_state = ManagerState.Preparing;
			m_prevState = m_state;
			m_connectionRequested = false;

			m_runThread = true;
			m_workerThread = new Thread(WorkerThread);
			m_workerThread.Start();

			ReplaceSpecialApplicationNameParts();

			if (configuration.AutoConnectDelay > 0) StartCoroutine(AutoConnectAsync());

			// auto-register objects that use MajorDomo
			IAutoRegister[] autoRegisterObjects = Resources.FindObjectsOfTypeAll<SynchronisedEntityBase>();
			foreach (var obj in autoRegisterObjects)
			{
				obj.RegisterWithMajorDomoManager();
			}
		}


		private void ReplaceSpecialApplicationNameParts()
		{
			string n = ClientName;

			if (n.Contains("{IPv") || n.Contains("{HOST}"))
			{
				string host = Dns.GetHostName();
				n = n.Replace("{HOST}", host);

				IPAddress[] addresses = Dns.GetHostAddresses(host);
				string ipv4 = "";
				string ipv6 = "";
				foreach (var addr in addresses)
				{
					if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						ipv4 = addr.ToString();
						if (ipv6.Length == 0)
						{
							ipv6 = addr.MapToIPv6().ToString();
						}
					}
					else if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
					{
						ipv6 = addr.ToString();
					}
				}
				n = n.Replace("{IPv4}", ipv4);
				n = n.Replace("{IPv6}", ipv6);
			}

			if (n.Contains("{USER}"))
			{
				n = n.Replace("{USER}", System.Environment.UserName);
			}

			if (n.Contains("{MACHINE}"))
			{
				n = n.Replace("{MACHINE}", System.Environment.MachineName);
			}

			if (n.Contains("{SCENE}"))
			{
				n = n.Replace("{SCENE}", SceneManager.GetActiveScene().name);
			}
			
			ClientName = n;
		}


		protected void WorkerThread()
		{
			m_state = ManagerState.Disconnected;

			m_client.OnClientRegistered   += ClientRegisteredHandler;
			m_client.OnClientUnregistered += ClientUnregisteredHandler;

			m_client.OnEntitiesPublished += EntitiesPublishedHandler;
			m_client.OnEntitiesRevoked   += EntitiesRevokedHandler;

			m_client.OnEntityControlChanged += EntityControlChangedHandler;

			m_client.OnClientBroadcastReceived += ClientBroadcastReceived;

			m_client.OnServerShutdown += ServerShutdownHandler;

			int configIdx = 0; // index to server infos when trying to connect

			while (m_runThread)
			{
				switch (m_state)
				{
					case ManagerState.Disconnected:
						if (m_connectionRequested)
						{
							// reset server info index
							configIdx = 0;
							m_state = ManagerState.Connecting;
						}
						break;

					case ManagerState.Connecting:
						if (configIdx < configuration.Servers.Count)
						{
							string address = configuration.Servers[configIdx].address;
							ushort port    = configuration.Servers[configIdx].port;
							float  timeout = configuration.Servers[configIdx].connectionTimeout;

							m_client.Connect(ClientName, address, port, timeout);
						}

						if (m_client.IsConnected())
						{
							// connection established, but maybe it was cancelled in the meantime?
							if (m_connectionRequested)
							{
								m_state = ManagerState.Connected;
							}
							else
							{
								m_client.Disconnect();
								m_state = ManagerState.Disconnected;
							}
						}
						else
						{
							if ((configIdx < configuration.Servers.Count - 1) && m_connectionRequested)
							{
								// not connected yet, but more servers in the list: continue
								configIdx++;
							}
							else
							{
								// all servers tried
								m_state = ManagerState.Disconnected;
								m_connectionRequested = false;
							}
						}
						break;

					case ManagerState.Connected:
						lock (m_entitiesToPublish)
						{
							if (m_entitiesToPublish.Count > 0)
							{
								m_client.PublishEntities(m_entitiesToPublish);
								m_entitiesToPublish.Clear();
							}
						}
						lock (m_entitiesToRevoke)
						{
							if (m_entitiesToRevoke.Count > 0)
							{
								m_client.RevokeEntities(m_entitiesToRevoke);
								m_entitiesToRevoke.Clear();
							}
						}
						lock (m_entitiesToControl)
						{
							if (m_entitiesToControl.Count > 0)
							{
								m_client.RequestEntityControl(m_entitiesToControl);
								m_entitiesToControl.Clear();
							}
						}
						lock (m_entitiesToRelease)
						{
							if (m_entitiesToRelease.Count > 0)
							{
								m_client.ReleaseEntityControl(m_entitiesToRelease);
								m_entitiesToRelease.Clear();
							}
						}
						m_client.ProcessClientUpdates();
						m_client.ProcessServerEvents();
						break;

					case ManagerState.Disconnecting:
						m_client.RevokeOwnEntites(false);
						m_client.Disconnect();
						m_state = ManagerState.Disconnected;
						break;

					default:
						break;
				}
				Thread.Sleep(10);
			}

			// end of thread: cleanup
			if (m_state != ManagerState.Disconnected)
			{
				m_client.RevokeOwnEntites(false);
				m_client.Disconnect();
				m_state = ManagerState.Disconnected;
			}
		}


		public void Update()
		{
			if (m_state == ManagerState.Connected && m_prevState == ManagerState.Connecting)
			{
				// client just connected: invoke event handlers
				OnClientConnected?.Invoke();
			}
			else if (!m_connectionRequested && m_prevState == ManagerState.Connected)
			{
				// client about to disconnect: invoke event handlers
				OnClientDisconnecting?.Invoke();
				// transition to actual disconnect
				m_state = ManagerState.Disconnecting;
			}

			if (m_client.IsConnected())
			{
				// process server updates and events here so entity updates are processed in the render loop
				m_client.ProcessServerUpdates();
			}

			// relay events to handlers in render loop:
			// registered clients
			lock (m_registeredClients)
			{
				// transfer list content to temp list to avoid deadlock through event handler using client functions
				m_processingClients.AddRange(m_registeredClients);
				m_registeredClients.Clear();
			}
			if (m_processingClients.Count > 0)
			{
				// process newly registered client events
				foreach (ClientData client in m_processingClients) OnClientRegistered?.Invoke(client);
				m_processingClients.Clear();
			}

			// unregistered clients
			lock (m_unregisteredClients)
			{
				// transfer list content to temp list to avoid deadlock through event handler using client functions
				m_processingClients.AddRange(m_unregisteredClients);
				m_unregisteredClients.Clear();
			}
			if (m_processingClients.Count > 0)
			{
				// process newly unregistered client events
				foreach (ClientData client in m_processingClients) OnClientUnregistered?.Invoke(client);
				m_processingClients.Clear();
			}
		
			// publishing entities
			lock (m_publishedEntities)
			{
				// transfer list content to temp list to avoid deadlock through event handler using client functions
				m_processingEntities.AddRange(m_publishedEntities);
				m_publishedEntities.Clear();
			}
			if (m_processingEntities.Count > 0)
			{
				// process published entity events
				OnEntitiesPublished?.Invoke(m_processingEntities);
				m_processingEntities.Clear();
			}

			// revoking entities
			lock (m_revokedEntities)
			{
				// transfer list content to temp list to avoid deadlock through event handler using client functions
				m_processingEntities.AddRange(m_revokedEntities);
				m_revokedEntities.Clear();
			}
			if (m_processingEntities.Count > 0)
			{
				// process revoked entity events
				OnEntitiesRevoked?.Invoke(m_processingEntities);
				m_processingEntities.Clear();
			}

			lock (m_controlledEntities)
			{
				// transfer list content to temp list to avoid deadlock through event handler using client functions
				m_processingEntities.AddRange(m_controlledEntities);
				m_controlledEntities.Clear();
			}
			if (m_processingEntities.Count > 0)
			{
				// process control-changed entity events
				OnEntityControlChanged?.Invoke(m_processingEntities);
				m_processingEntities.Clear();
			}

			// broadcasts
			lock (m_broadcasts)
			{
				// transfer list content to temp list to avoid deadlock through event handler using client functions
				m_processingBroadcasts.AddRange(m_broadcasts);
				m_broadcasts.Clear();
			}
			if (m_processingBroadcasts.Count > 0)
			{
				// handle broadcasts
				foreach (Broadcast broadcast in m_processingBroadcasts)
				{
					OnClientBroadcastReceived?.Invoke(broadcast);
				}
				m_processingBroadcasts.Clear();
			}

			lock (m_serverShutdown)
			{
				if (m_serverShutdown.Count > 0)
				{
					bool restart = m_serverShutdown[0];
					OnServerShutdown?.Invoke(restart);
					StartCoroutine(AutoDisconnectAsync(restart));
					m_serverShutdown.Clear();
				}
			}

			m_prevState = m_state;
		}


		public void Connect()
		{
			m_connectionRequested = true;
		}


		public void Disconnect()
		{
			m_connectionRequested = false;
		}


		public void OnApplicationQuit()
		{
			// just to be sure...
			m_runThread = false;
			m_workerThread?.Join();
			m_workerThread = null;
		}


		protected void ClientRegisteredHandler(ClientData _client)
		{
			lock (m_registeredClients) { m_registeredClients.Add(_client); }
		}

		protected void ClientUnregisteredHandler(ClientData _client)
		{
			lock (m_unregisteredClients) { m_unregisteredClients.Add(_client); }
		}

		protected void EntitiesPublishedHandler(List<EntityData> _entities)
		{
			lock (m_publishedEntities) { m_publishedEntities.AddRange(_entities); }
		}

		protected void EntitiesRevokedHandler(List<EntityData> _entities)
		{
			lock (m_revokedEntities) { m_revokedEntities.AddRange(_entities); }
		}

		protected void EntityControlChangedHandler(List<EntityData> _entities)
		{
			lock (m_controlledEntities) { m_controlledEntities.AddRange(_entities); }
		}

		protected void ClientBroadcastReceived(Broadcast _broadcast)
		{
			lock(m_broadcasts) { m_broadcasts.Add(_broadcast); }
		}

		protected void ServerShutdownHandler(bool _reboot)
		{
			lock(m_serverShutdown) { m_serverShutdown.Add(_reboot); }
		}


		private IEnumerator AutoConnectAsync()
		{
			yield return new WaitForSeconds(configuration.AutoConnectDelay);
			Connect();
		}


		private IEnumerator AutoDisconnectAsync(bool restart)
		{
			yield return new WaitForSeconds(configuration.AutoDisconnectDelay);
			Disconnect();
			yield return new WaitForSeconds(configuration.AutoReconnectDelay);
			Connect();
		}


		protected enum ManagerState
		{
			Preparing,
			Disconnected,
			Connecting,
			Connected,
			Disconnecting
		}


		public void LoadConfiguration()
		{
			configuration.LoadConfiguration();
		}


		public void SaveConfiguration()
		{
			configuration.SaveConfiguration();
		}


		private static MajorDomoManager ms_instance = null;

		protected ManagerState    m_state, m_prevState;
		protected bool            m_connectionRequested;
		protected MajorDomoClient m_client;
		protected Thread          m_workerThread;
		protected bool            m_runThread;

		protected List<EntityData> m_entitiesToPublish;
		protected List<EntityData> m_entitiesToRevoke;
		protected List<EntityData> m_entitiesToControl;
		protected List<EntityData> m_entitiesToRelease;

		protected List<ClientData> m_registeredClients;
		protected List<ClientData> m_unregisteredClients;
		protected List<ClientData> m_processingClients;

		protected List<EntityData> m_publishedEntities;
		protected List<EntityData> m_revokedEntities;
		protected List<EntityData> m_controlledEntities;
		protected List<EntityData> m_processingEntities;

		protected List<Broadcast>  m_broadcasts;
		protected List<Broadcast>  m_processingBroadcasts;
		protected List<bool>       m_serverShutdown;

		static private Rect m_rectConnectBtnPos = new Rect(10, 10, 100, 30);
	}
}