#region Copyright Information
// Sentience Lab Unity Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SentienceLab.MoCap
{
	/// <summary>
	/// Component for a Motion Capture manager.
	/// One instance of this object is needed that all MoCap controlled objects get their data from.
	/// Make sure the MoCapManager script is executed before any other script in "Project Settings/Script Execution Order".
	/// </summary>

	[DisallowMultipleComponent]
	[AddComponentMenu("Motion Capture/MoCap Manager")]

	public class MoCapManager : MonoBehaviour
	{
		[System.Serializable]
		public class Configuration : ConfigFileBase
		{
			public Configuration() : base("MoCapConfig.txt", "MoCap") { }

			public List<string> Sources;
		}

		[Tooltip("Name of the MoCap Client\n" +
		"Special strings:\n" +
			"  {IPv4}   : IPv4 Address of the client\n" +
			"  {IPv6}   : IPv6 Address of the client\n" +
			"  {HOST}   : Hostname of the client\n" +
			"  {MACHINE}: Machine name of the client\n" +
			"  {USER}   : User name\n" +
			"  {SCENE}  : Scene name"
		)]
		public string ClientName = "{SCENE}_{HOST}";

		[ContextMenuItem("Load configuration from config file", "LoadConfiguration")]
		[ContextMenuItem("Save configuration to config file", "SaveConfiguration")]
		public Configuration configuration;

		[Tooltip("Input action for pausing/running the client")]
		public InputActionProperty PauseAction;


		private readonly byte[] clientAppVersion = new byte[] { 1, 4, 5, 0 };


		/// <summary>
		/// Called once at the start of the scene. 
		/// </summary>
		/// 
		public void Awake()
		{
			CreateManager(); // trigger creation of singleton (if not already happened)

			if (ms_instance == null)
			{
				ms_instance = this;
				Debug.Log("MoCap Manager instance created (" + m_client.GetDataSourceName() + ")");
			}
			else
			{
				// there can be only one instance
				GameObject.Destroy(this);
			}

			if (PauseAction != null)
			{
				PauseAction.action.performed += OnPausedActionPerformed;
				PauseAction.action.Enable();
			}

			m_pauseClient = false;

			m_lastUpdateFrame    = -1;
			m_lastPreRenderFrame = -1;
		}


		/// <summary>
		/// Called when object is about to be destroyed.
		/// Disconnects from the NatNet server and destroys the NatNet client.
		/// </summary>
		///
		public void OnDestroy()
		{
			m_clientMutex.WaitOne();
			if (m_client != null)
			{
				m_client.Disconnect();
				m_client = null;

				ms_sceneListeners.Clear();
			}
			m_clientMutex.ReleaseMutex();
			// MoCap Objects might have registered Coroutines here
			StopAllCoroutines();
		}


		/// <summary>
		/// Called when the application is paused or continued.
		/// </summary>
		/// <param name="pause"><c>true</c> when the application is paused</param>
		/// 
		public void OnApplicationPause(bool pause)
		{
			if (m_client != null)
			{
				m_client.SetPaused(pause);
			}
		}


		/// <summary>
		/// Called when the application is paused or continued.
		/// </summary>
		/// <param name="pause"><c>true</c> when the application is paused</param>
		/// 
		public void OnPausedActionPerformed(InputAction.CallbackContext _ctx)
		{
			m_pauseClient = !m_pauseClient;
			OnApplicationPause(m_pauseClient);
		}


		/// <summary>
		/// Called once per rendered frame. 
		/// </summary>
		///
		public void Update()
		{
			if (m_lastUpdateFrame < Time.frameCount)
			{
				UpdateScene();
				m_lastUpdateFrame = Time.frameCount;
			}
		}


		/// <summary>
		/// Called just before the scene renders. 
		/// </summary>
		///
		public void OnPreRender()
		{
			if (m_lastPreRenderFrame < Time.frameCount)
			{
				UpdateScene();
				m_lastPreRenderFrame = Time.frameCount;
			}
		}


		/// <summary>
		/// Get new scene data now.
		/// </summary>
		///
		public void UpdateScene()
		{
			if (m_client != null)
			{
				if (m_client.IsConnected())
				{
					bool dataChanged  = false;
					bool sceneChanged = false;
					m_client.Update(ref dataChanged, ref sceneChanged);

					if (sceneChanged) NotifyListeners_Change(Scene);
					if (dataChanged ) NotifyListeners_Update(Scene);
				}
			}
		}


		/// <summary>
		/// Checks if the client is connected to the MotionServer.
		/// </summary>
		/// <returns><c>true</c> if the client is connected</returns>
		/// 
		public bool IsConnected
		{
			get
			{
				return (m_client != null) && m_client.IsConnected();
			}
		}

		/// <summary>
		/// Gets the name of the connected data source.
		/// </summary>
		/// <returns>Name of the connected data source</returns>
		/// 
		public string DataSourceName
		{
			get
			{
				return (m_client != null) ? m_client.GetDataSourceName() : "";
			}
		}


		/// <summary>
		/// Gets the amount of frames per second that the MoCap system provides.
		/// </summary>
		/// <returns>Update rate of the Mocap system in frames per second</returns>
		/// 
		public float Framerate
		{
			get
			{
				return (m_client != null) ? m_client.GetFramerate() : 0.0f;
			}
		}

		/// <summary>
		/// Gets the latest scene data structure.
		/// </summary>
		/// <returns>Scene data or <c>null</c> if client is not connected</returns>
		/// 
		public Scene Scene {
			get
			{
				return (m_client != null) ? m_client.GetScene() : null;
			}
		}


		/// <summary>
		/// Adds a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to add</param>
		/// <returns><c>true</c>, if the scene listener was added, <c>false</c> otherwise.</returns>
		/// 
		public bool AddSceneListener(SceneListener listener)
		{
			bool added = false;
			if (!ms_sceneListeners.Contains(listener))
			{
				ms_sceneListeners.Add(listener);
				added = true;
				// immediately trigger callback
				if (m_client != null)
				{
					Scene scene = Scene;
					scene.mutex.WaitOne();
					listener.SceneDefinitionChanged(scene);
					scene.mutex.ReleaseMutex();
				}
			}
			return added;
		}


		/// <summary>
		/// Removes a scene data listener.
		/// </summary>
		/// <param name="listener">The listener to remove</param>
		/// <returns><c>true</c>, if the scene listener was removed, <c>false</c> otherwise.</returns>
		/// 
		public bool RemoveSceneListener(SceneListener listener)
		{
			return ms_sceneListeners.Remove(listener);
		}


		/// <summary>
		/// Notifies scene listeners of an update.
		/// </summary>
		/// <param name="scene"> the scene has been updated</param>
		/// 
		private void NotifyListeners_Update(Scene scene)
		{
			scene.mutex.WaitOne();
			// pump latest data through the buffers before calling listeners
			foreach (Actor a in scene.actors)
			{
				foreach (Marker m in a.markers) { m.buffer.Push(); }
				foreach (Bone   b in a.bones)   { b.buffer.Push(); }
			}

			// call listeners
			foreach (SceneListener listener in ms_sceneListeners)
			{
				listener.SceneDataUpdated(scene);
			}
			scene.mutex.ReleaseMutex();
		}


		/// <summary>
		/// Notifies scene listeners of a description change.
		/// </summary>
		/// <param name="scene"> the scene has been changed</param>
		/// 
		private void NotifyListeners_Change(Scene scene)
		{
			scene.mutex.WaitOne();
			foreach (SceneListener listener in ms_sceneListeners)
			{
				listener.SceneDefinitionChanged(scene);
			}
			scene.mutex.ReleaseMutex();
		}


		/// <summary>
		/// Gets the internal NatNet client instance singelton.
		/// When creating the singleton for the first time, 
		/// tries to connect to a local MoCap server, and if not successful, a remote MoCap server.
		/// </summary>
		/// 
		private void CreateManager()
		{
			// no sources listed? > load config file
			if (configuration.Sources.Count == 0)
			{
				configuration.LoadConfiguration();
			}

			ms_sceneListeners = new List<SceneListener>();

			m_clientMutex.WaitOne();
			if (m_client == null)
			{
				// only connect when this script is actually enabled
				if (this.isActiveAndEnabled)
				{
					// construct client name
					ReplaceSpecialApplicationNameParts();
					
					// build list of data sources
					ICollection<IMoCapClient_ConnectionInfo> sources = GetSourceList();

					// run through the list
					foreach (IMoCapClient_ConnectionInfo info in sources)
					{
						// construct client according to structure (this is ugly...)
						if (info is NatNetClient.ConnectionInfo)
						{
							// is client already the right type?
							if (!(m_client is NatNetClient))
							{
								m_client = new NatNetClient(ClientName, clientAppVersion);
							}
						}
						else if (info is FileClient.ConnectionInfo) 
						{
							// is client already the right type?
							if (!(m_client is FileClient))
							{
								m_client = new FileClient();
							}
						}

						if (m_client.Connect(info))
						{
							// connection established > that's it
							break;
						}
					}

					if ((m_client != null) && m_client.IsConnected())
					{
						Debug.Log("MoCap client connected to " + m_client.GetDataSourceName() + ".\n" +
						          "Framerate: " + m_client.GetFramerate() + " fps");

						// print list of actor and device names
						Scene scene = m_client.GetScene();
						if (scene.actors.Count > 0)
						{
							string actorNames = "";
							foreach (Actor a in scene.actors)
							{
								if (actorNames.Length > 0) { actorNames += ", "; }
								actorNames += a.name;
							}
							Debug.Log("Actors (" + scene.actors.Count + "): " + actorNames);
						}
						if (scene.devices.Count > 0)
						{
							string deviceNames = "";
							foreach (Device d in scene.devices)
							{
								if (deviceNames.Length > 0) { deviceNames += ", "; }
								deviceNames += d.name;
							}
							Debug.Log("Devices (" + scene.devices.Count + "): " + deviceNames);
						}
					}
				}

				if ((m_client == null) || !m_client.IsConnected())
				{
					// not active or not able to connect to any data source: create dummy singleton 
					m_client = new DummyClient();
				}

				// all fine, notify listeners of scene change
				if ((m_client != null) && m_client.IsConnected())
				{
					NotifyListeners_Change(Scene);
				}
			}
			m_clientMutex.ReleaseMutex();
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


		/// <summary>
		/// Reads the MoCap data source file asset and constructs a list of the connection information.
		/// </summary>
		/// <returns>List of IP addresses to query</returns>
		/// 
		private ICollection<IMoCapClient_ConnectionInfo> GetSourceList()
		{
			LinkedList<IMoCapClient_ConnectionInfo> sources = new LinkedList<IMoCapClient_ConnectionInfo>();

			// construct sources list with connection data structures
			foreach (string source in configuration.Sources)
			{
				if (source.Contains("/"))
				{
					// slash can only be in a filename
					sources.AddLast(new FileClient.ConnectionInfo(source));
				}
				else
				{
					// or is it an IP address
					IPAddress address;
					if (IPAddress.TryParse(source.Trim(), out address))
					{
						// success > add to list
						sources.AddLast(new NatNetClient.ConnectionInfo(address));
					}
				}
			}
			return sources;
		}


		/// <summary>
		/// Searches for the MoCapManager instance in the scene and returns it
		/// or quits if it is not defined.
		/// </summary>
		/// <returns>the MoCapManager instance</returns>
		/// 
		public static MoCapManager Instance
		{
			get
			{
				if (ms_instance == null)
				{
					if (!m_warningIssued)
					{
						Debug.LogWarning("No MoCapManager in scene");
						m_warningIssued = true;
					}
				}

				return ms_instance;
			}
		}


		public void LoadConfiguration()
		{
			configuration.LoadConfiguration();
		}


		public void SaveConfiguration()
		{
			configuration.SaveConfiguration();
		}


		private static MoCapManager        ms_instance       = null;
		private static List<SceneListener> ms_sceneListeners = null;

		private static bool         m_warningIssued = false;
		private static IMoCapClient m_client        = null;
		private static Mutex        m_clientMutex   = new Mutex();

		private long m_lastUpdateFrame, m_lastPreRenderFrame;

		private bool m_pauseClient; 
	}

}
