#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace SentienceLab.MajorDomo
{
	[AddComponentMenu("MajorDomo/Synchronised GameObject")]
	public class SynchronisedGameObject : MonoBehaviour
	{
		public static readonly string GAMEOBJECT_AUTO_NAME = "{GAMEOBJECT}";

		[Tooltip("Name of the entity to register with the server.\n" +
				 "Leave empty to use this game object's name.\n" +
				 "The string \"{GAMEOBJECT}\" will be automatically replaced by the game object's name.")]
		public string EntityName = GAMEOBJECT_AUTO_NAME;

		[Tooltip("Synchronisation mode for the entity.\n" 
			+ "Client: object is controlled by the client.\n" 
			+ "Server: object is controlled by the server/another client\n"
			+ "Shared: object control can be shared among clients")]
		public ESynchronisationMode SynchronisationMode = ESynchronisationMode.Client;

		[Tooltip("Entity stays on the server when the client disconnects")]
		public bool Persistent = false;

		[Tooltip("If enabled, the 'active' flag is synchronised and loss off connection to the server can disable the object")]
		public bool CanDisableGameObject = true;

		[Tooltip("Explicit list of components to synchronise. If empty, use all existing components found in this game object.")]
		[FormerlySerializedAs("Components")]
		public List<AbstractSynchronisedComponent> SynchronisedComponents;

		// number of frames to wait after another client has taken over control before trying to take control back
		const int CONTROL_COOLDOWN_COUNT = 10;



		/// <summary>
		/// Mode of synchronisation. Which part of the connection is the "master" and in control.
		/// </summary>
		/// 
		public enum ESynchronisationMode
		{
			Client,
			Server,
			Shared
		}

		/// <summary>
		/// What components of this game object's transform are synchronised.
		/// </summary>
		public enum ETransformComponents
		{
			TranslationRotationScale,
			TranslationRotation,
			Translation,
			Rotation
		}

		/// <summary>
		/// Possible actions for when the synchronisation is lost.
		/// </summary>
		public enum ESyncLostBehaviour
		{
			/// <summary>
			/// Freeze the object at the last synchronised position/orientation.
			/// </summary>
			Freeze,

			/// <summary>
			/// Disable the game object and re-enable when tracking continues.
			/// </summary>
			Disable
		};


		public bool IsControlledByClient
		{
			get
			{
				if (m_entity == null) return false;
				if (MajorDomoManager.Instance == null) return false;
				if (!MajorDomoManager.Instance.IsConnected()) return false;
				return m_entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID);
			}
			private set { }
		}


		public bool IsControlledByServer
		{
			get
			{
				if (m_entity == null) return false;
				if (MajorDomoManager.Instance == null) return false;
				if (!MajorDomoManager.Instance.IsConnected()) return false;
				return !m_entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID);
			}
			private set { }
		}



		public void Awake()
		{
			if (MajorDomoManager.Instance == null)
			{
				Debug.LogWarning("MajorDomoManager component needed");
				this.enabled = false;
				return;
			}
			else
			{
				if (MainThreadTaskDispatcher.Instance == null)
				{
					// this component is needed as well
					MajorDomoManager.Instance.gameObject.AddComponent<MainThreadTaskDispatcher>();
				}
			}
		}


		public void Start()
		{
			// start registering in next frame
			StartCoroutine(RegisterWithMajorDomoManager());
		}


		protected IEnumerator RegisterWithMajorDomoManager()
		{
			if (!m_registered)
			{
				m_registered = true; 

				// wait one frame before registering
				yield return null;

				m_entity = null;
				m_controlChangeCooldown = 0;
				m_oldControlledByClient = IsControlledByClient;

				// empty name > use game object name
				if (EntityName.Trim().Length == 0)
				{
					EntityName = gameObject.name;
				}
				CheckEntityNameReplacements();

				// no submodules given explicitely, search for them
				if (SynchronisedComponents == null)
				{
					SynchronisedComponents = new List<AbstractSynchronisedComponent>();
				}
				if (SynchronisedComponents.Count == 0)
				{
					SynchronisedComponents.AddRange(GetComponents<AbstractSynchronisedComponent>());
				}

				//Debug.LogFormat("Registering entity '{0}' with MajorDomo client.", EntityName);

				MajorDomoManager.Instance.OnClientUnregistered   += ClientEventCallback;
				MajorDomoManager.Instance.OnEntitiesPublished    += EntityEventCallback;
				MajorDomoManager.Instance.OnEntitiesRevoked      += EntityEventCallback;
				MajorDomoManager.Instance.OnEntityControlChanged += EntityEventCallback;

				if (SynchronisationMode == ESynchronisationMode.Server)
				{
					if (CanDisableGameObject) gameObject.SetActive(false);
				}

				CheckEntity();
			}
		}


		public void Update()
		{
			if ((m_entity == null) || (m_entity.State != EntityData.EntityState.Registered)) return;

			if ((SynchronisationMode == ESynchronisationMode.Client) ||
				(SynchronisationMode == ESynchronisationMode.Shared))
			{
				// client control
				if (IsModified() && IsControlledByClient)
				{
					SynchroniseToEntity();
					ResetModified();
				}
			}

			if ((SynchronisationMode == ESynchronisationMode.Server) ||
				(SynchronisationMode == ESynchronisationMode.Shared))
			{
				if (IsControlledByServer && m_entity.IsUpdated())
				{
					SynchroniseFromEntity(false);
					m_entity.ResetUpdated();
					// every entity update resets the control change timeout
					m_controlChangeCooldown = CONTROL_COOLDOWN_COUNT;
				}
				else if (IsControlledByClient && m_entity.IsModified())
				{
					// special case if you are synchronising an entity that belongs to this client:
					// since no updates are sent to the client itself, we have to "manually" sync based on the "modified" flag
					SynchroniseFromEntity(false);
				}
				else if (SynchronisationMode == ESynchronisationMode.Shared)
				{
					if (IsModified() && (m_controlChangeCooldown == 0))
					{
						if (IsControlledByServer)
						{
							// no server updates for some time and client wants to take control
							MajorDomoManager.Instance.RequestControl(m_entity);
						}
					}
				}
			}

			if (m_controlChangeCooldown > 0)
			{
				// prevent control takeover during cooldown period
				ResetModified();
				m_controlChangeCooldown--;
			}
		}


		private void EntityUpdated(EntityData _)
		{
			// this might have been called from a networking thread,
			// so relay the Update call to the main thread
			MainThreadTaskDispatcher.Instance.Add(Update);
		}


		public void OnEnable()
		{
			if (m_entity == null) return;

			// make sure current state is synchronised
			if (  (SynchronisationMode == ESynchronisationMode.Client) ||
				( (SynchronisationMode == ESynchronisationMode.Shared) && IsControlledByClient))
			{
				SynchroniseToEntity();
				ResetModified();
			}
			else if (  (SynchronisationMode == ESynchronisationMode.Server) ||
					 ( (SynchronisationMode == ESynchronisationMode.Shared) && IsControlledByServer))
			{
				SynchroniseFromEntity(false);
				m_entity.ResetUpdated();
			}
		}


		public void OnDisable()
		{
			if (m_entity == null) return;

			// make sure last state is synchronised
			if ((SynchronisationMode == ESynchronisationMode.Client) ||
				((SynchronisationMode == ESynchronisationMode.Shared) && IsControlledByClient))
			{
				SynchroniseToEntity();
				ResetModified();
			}
		}


		public void OnDestroy()
		{
			if (m_registered)
			{
				if (MajorDomoManager.Instance != null)
				{
					MajorDomoManager.Instance.OnClientUnregistered   -= ClientEventCallback;
					MajorDomoManager.Instance.OnEntitiesPublished    -= EntityEventCallback;
					MajorDomoManager.Instance.OnEntitiesRevoked      -= EntityEventCallback;
					MajorDomoManager.Instance.OnEntityControlChanged -= EntityEventCallback;

					if (m_entity != null)
					{
						MajorDomoManager.Instance.RevokeEntity(m_entity);
					}
				}

				m_entity     = null;
				m_registered = false;
			}
		}


		protected void ClientEventCallback(ClientData _)
		{
			CheckEntity();
		}


		protected void EntityEventCallback(List<EntityData> _)
		{
			CheckEntity();
		}


		protected void CheckEntity()
		{
			if (m_entity == null)
			{
				// entity not found/created yet. Let's search first
				CheckEntityNameReplacements();
				m_entity = MajorDomoManager.Instance.FindEntity(EntityName);
				if (m_entity != null)
				{
					// found it > find the variables, too
					FindEntityVariables();
					// adjust flags from entity
					Persistent = m_entity.IsPersistent();

					// client+server and NOT shared doesn't work 
					if (SynchronisationMode == ESynchronisationMode.Shared && !m_entity.AllowsSharedControl())
					{
						Debug.LogWarningFormat("'{0}' mode on non-shared entity '{1}' not possible. Switching to '{2}' only.",
							SynchronisationMode, m_entity.ToString(true, true), ESynchronisationMode.Server);
						SynchronisationMode = ESynchronisationMode.Server;
					}

					if ((SynchronisationMode != ESynchronisationMode.Client) || Persistent)
					{
						// server authority or persistent > update immediately
						SynchroniseFromEntity(true);
					}

					m_entity.OnEntityUpdated += EntityUpdated;

					Debug.LogFormat("'{0}' synchronised with entity '{1}'",
						gameObject.name, m_entity.ToString(true, true));
				}
				else
				{
					// entity not found. let's create and publish it
					// either when in client mode, or when in shared+server mode
					if (MajorDomoManager.Instance.IsConnected() &&
					    (SynchronisationMode != ESynchronisationMode.Server) )
					{
						// Create and publish entity and then wait for the callback when server has acknowledged
						// then find the actual variables in the acknowledged entity version
						CheckEntityNameReplacements();
						EntityData entity = MajorDomoManager.Instance.CreateClientEntity(EntityName);
						if (entity != null)
						{
							CreateEntityVariables(entity);

							entity.AllowSharedControl(SynchronisationMode == ESynchronisationMode.Shared);
							entity.SetPersistent(Persistent);

							MajorDomoManager.Instance.PublishEntity(entity);
							Debug.LogFormat("Publishing entity '{0}'",
								entity.ToString(true, true));
						}
					}
				}
			}
			else if (m_entity.State == EntityData.EntityState.Revoked)
			{
				// entity was revoked > remove references

				Debug.LogFormat("'{0}' lost synchronisation with entity '{1}'",
					gameObject.name, m_entity.ToString(true, false));

				DestroyEntityVariables();

				if (IsControlledByServer)
				{
					if (CanDisableGameObject) gameObject.SetActive(false);
				}

				m_entity = null;
			}
			else if (m_oldControlledByClient != IsControlledByClient)
			{
				Debug.LogFormat("Control changed for entity '{0}' to '{1}'",
					m_entity.ToString(true, false), IsControlledByClient ? "client" : "server");
				m_oldControlledByClient = IsControlledByClient;
				m_controlChangeCooldown = m_oldControlledByClient ? 0 : CONTROL_COOLDOWN_COUNT;
			}
		}


		private void CheckEntityNameReplacements()
		{
			// replace template string
			EntityName = EntityName.Replace(GAMEOBJECT_AUTO_NAME, this.gameObject.name);
		}


		protected void CreateEntityVariables(EntityData _entity)
		{
			// transform variables
			if (CanDisableGameObject)
			{
				_entity.AddValue_Boolean(EntityValue.ENABLED, gameObject.activeSelf);
			}

			foreach (var cmp in SynchronisedComponents)
			{
				cmp.CreateEntityVariables(_entity);
			}
		}


		protected void FindEntityVariables()
		{
			m_valEnabled = CanDisableGameObject ? m_entity.GetValue_Boolean(EntityValue.ENABLED) : null;

			foreach (var cmp in SynchronisedComponents)
			{
				cmp.FindEntityVariables(m_entity);
			}
		}


		protected void DestroyEntityVariables()
		{
			m_valEnabled = null;

			foreach (var cmp in SynchronisedComponents)
			{
				cmp.DestroyEntityVariables();
			}
		}


		protected void SynchroniseFromEntity(bool _initialise)
		{
			// Synchronise enable flag
			if (m_valEnabled != null && CanDisableGameObject)
			{
				gameObject.SetActive(m_valEnabled.Value);
			}
			else
			{
				gameObject.SetActive(true);
			}
			// synchronise components
			foreach (var cmp in SynchronisedComponents)
			{
				cmp.SynchroniseFromEntity(_initialise);
			}
		}


		protected bool IsModified()
		{
			foreach (var cmp in SynchronisedComponents)
			{
				if (cmp.IsModified()) return true;
			}
			return false;
		}


		protected void SynchroniseToEntity()
		{
			// Synchronise enable flag
			if (m_valEnabled != null)
			{
				m_valEnabled.Modify(gameObject.activeSelf);
			}
			// Synchronise components
			foreach (var cmp in SynchronisedComponents)
			{
				cmp.SynchroniseToEntity();
			}
		}


		protected void ResetModified()
		{
			foreach (var cmp in SynchronisedComponents)
			{
				cmp.ResetModified();
			}
		}


		public bool Registered
		{
			get
			{
				return m_entity.State == EntityData.EntityState.Registered;
			}
		}


		private bool m_registered = false;

		private bool m_oldControlledByClient;
		private int  m_controlChangeCooldown = 0;

		private EntityData          m_entity;
		private EntityValue_Boolean m_valEnabled;
	}
}
