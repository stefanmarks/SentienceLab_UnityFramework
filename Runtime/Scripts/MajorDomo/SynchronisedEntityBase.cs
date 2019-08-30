#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;

namespace SentienceLab.MajorDomo
{
	public abstract class SynchronisedEntityBase : MonoBehaviour, MajorDomoManager.IAutoRegister
	{
		[Tooltip("Name of the entity to register with the server.\n" +
				 "Leave empty to use this game object's name.\n" +
				 "The string \"{GAMEOBJECT}\" will be automatically replaced by the game object name.")]
		public string EntityName = "";

		/// <summary>
		/// Mode of synchronisation. Which part of the connection is the "master" and in control.
		/// </summary>
		/// 
		public enum ESynchronisationMode
		{
			Client,
			Server,
			ClientAndServer
		}

		[Tooltip("Mode for the entity.\nClient: object is controlled by the client.\nServer: objects is controlled by the server/another client")]
		public ESynchronisationMode SynchronisationMode = ESynchronisationMode.Client;

		public bool IsControlledByClient {
			get {
				return 
					(Entity != null) && (MajorDomoManager.Instance != null) &&
					MajorDomoManager.Instance.IsConnected() && 
					Entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID);
			}
			private set { }
		}

		[Tooltip("Entity control can be shared with other clients")]
		public bool SharedControl = false;

		[Tooltip("Entity stays on the server when the client disconnects")]
		public bool Persistent = false;


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
				RegisterWithMajorDomoManager(); // just to be sure
			}
		}


		public void RegisterWithMajorDomoManager()
		{
			if (m_initialised) return;

			Initialise();
			m_initialised = true;

			// empty name > use game object name
			if (EntityName.Trim().Length == 0)
			{
				EntityName = gameObject.name;
			}
			// replace template string
			EntityName = EntityName.Replace("{GAMEOBJECT}", this.gameObject.name);

			Entity = null;
			m_controlChangeCooldown = 0;
			m_oldControlledByClient = IsControlledByClient;

			MajorDomoManager.Instance.OnClientUnregistered   += delegate (ClientData _c)       { CheckEntity(); };
			MajorDomoManager.Instance.OnEntitiesPublished    += delegate (List<EntityData> _l) { CheckEntity(); };
			MajorDomoManager.Instance.OnEntitiesRevoked      += delegate (List<EntityData> _l) { CheckEntity(); };
			MajorDomoManager.Instance.OnEntityControlChanged += delegate (List<EntityData> _l) { CheckEntity(); };

			if (SynchronisationMode == ESynchronisationMode.Server)
			{
				if (CanDisableGameObject()) gameObject.SetActive(false);
			}
		}


		public void Update()
		{
			if ((Entity == null) || (Entity.State != EntityData.EntityState.Registered)) return;
			
			if ((SynchronisationMode == ESynchronisationMode.Server) || 
				(SynchronisationMode == ESynchronisationMode.ClientAndServer))
			{
				if (!IsControlledByClient && Entity.IsUpdated())
				{
					SynchroniseFromEntity();
					Entity.ResetUpdated();
					m_controlChangeCooldown = 10;
				}
				else if (SynchronisationMode == ESynchronisationMode.ClientAndServer)
				{
					if (IsModified() && (m_controlChangeCooldown == 0))
					{
						if (!IsControlledByClient)
						{
							// no updates for some time and client wants to take control
							MajorDomoManager.Instance.RequestControl(Entity);
						}
					}
				}
				if (m_controlChangeCooldown > 0)
				{
					// prevent control takeover from old modified state
					ResetModified();
				}
			}
			
			if ((SynchronisationMode == ESynchronisationMode.Client) ||
				(SynchronisationMode == ESynchronisationMode.ClientAndServer))
			{
				// client control
				if (IsModified() && IsControlledByClient)
				{
					SynchroniseToEntity();
					ResetModified();
				}
			}

			if (m_controlChangeCooldown > 0) 
			{
				m_controlChangeCooldown--;
			}
		}


		public void OnDisable()
		{
			// make sure last state is synchronised
			if ((SynchronisationMode == ESynchronisationMode.Client) || 
				((SynchronisationMode == ESynchronisationMode.ClientAndServer) && IsControlledByClient))
			{
				SynchroniseToEntity();
			}
		}


		public void OnEnable()
		{
			// make sure current state is synchronised
			if ((SynchronisationMode == ESynchronisationMode.Client) ||
				((SynchronisationMode == ESynchronisationMode.ClientAndServer) && IsControlledByClient))
			{
				SynchroniseToEntity();
			}
			else
			{
				SynchroniseFromEntity();
			}
		}


		protected void CheckEntity()
		{
			if (Entity == null)
			{
				// entity not found/created yet. Let's search first
				Entity = MajorDomoManager.Instance.FindEntity(EntityName);
				if (Entity != null)
				{
					// found it > find the variables, too
					FindVariables();
					// adjust flags from entity
					Persistent    = Entity.IsPersistent();
					SharedControl = Entity.AllowsSharedControl();

					// client+server and NOT shared doesn't work 
					if (SynchronisationMode == ESynchronisationMode.ClientAndServer && !SharedControl)
					{
						Debug.LogWarningFormat("'{0}' mode on non-shared entity '{1}' not possible. Switching to '{2}' only.",
							ESynchronisationMode.ClientAndServer, Entity.ToString(true, true), ESynchronisationMode.Server);
						SynchronisationMode = ESynchronisationMode.Server;
					}

					if ((SynchronisationMode == ESynchronisationMode.Server) || 
						(SynchronisationMode == ESynchronisationMode.ClientAndServer) || Persistent)
					{
						// server authority or persistent > update immediately
						SynchroniseFromEntity();
					}

					Entity.OnEntityUpdated += Update;

					Debug.LogFormat("{0} '{1}' synchronised with entity '{2}'",
						GetEntityTypeName(), gameObject.name, Entity.ToString(true, true));
				}
				else
				{
					// entity not found. let's create and publish it
					// either when in client mode, or when in shared+server mode
					if (MajorDomoManager.Instance.IsConnected() &&
						( (SynchronisationMode == ESynchronisationMode.Client) ||
						  ((SynchronisationMode == ESynchronisationMode.ClientAndServer) && SharedControl) ) )
					{
						// Create and publish entity and then wait for the callback when server has acknowledged
						// then find the actual variables in the acknowledged entity version
						EntityData entity = MajorDomoManager.Instance.CreateClientEntity(EntityName);
						if (entity != null)
						{
							CreateVariables(entity);

							entity.AllowSharedControl(SharedControl);
							entity.SetPersistent(Persistent);

							MajorDomoManager.Instance.PublishEntity(entity);
							Debug.LogFormat("Publishing {0} entity '{1}'",
								GetEntityTypeName(), entity.ToString(true, true));
						}
					}
				}
			}
			else if (Entity.State == EntityData.EntityState.Revoked)
			{
				// entity was revoked > remove references

				Debug.LogFormat("{0} '{1}' lost synchronisation with entity '{2}'",
					GetEntityTypeName(), gameObject.name, Entity.ToString(true, false));

				DestroyVariables();

				if (!IsControlledByClient)
				{
					if (CanDisableGameObject()) gameObject.SetActive(false);
				}

				Entity = null;
			}
			else if (m_oldControlledByClient != IsControlledByClient)
			{
				Debug.LogFormat("Control changed for entity '{0}' to '{1}'",
					Entity.ToString(true, false), IsControlledByClient ? "client" : "server");
				m_oldControlledByClient = IsControlledByClient;
				m_controlChangeCooldown = 100;
			}
		}


		protected abstract void Initialise();

		protected abstract void CreateVariables(EntityData _entity);

		protected abstract void FindVariables();

		protected abstract void DestroyVariables();

		protected abstract void SynchroniseFromEntity();

		protected abstract bool IsModified();

		protected abstract void SynchroniseToEntity();

		protected abstract void ResetModified();

		protected abstract bool CanDisableGameObject();

		protected abstract string GetEntityTypeName();

		private bool m_initialised = false;

		private bool m_oldControlledByClient;
		private int  m_controlChangeCooldown = 0;

		protected EntityData Entity { get; private set; }
	}
}
