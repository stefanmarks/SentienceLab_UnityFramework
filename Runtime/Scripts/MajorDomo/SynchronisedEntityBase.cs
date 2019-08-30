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

		[Tooltip("Mode for the entity.\nClient: object is controlled by the client.\nServer: objects is controlled by the server/another client")]
		public MajorDomoManager.SynchronisationMode SynchronisationMode = MajorDomoManager.SynchronisationMode.Client;

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

			MajorDomoManager.Instance.OnClientUnregistered   += delegate (ClientData _c)       { CheckEntity(); };
			MajorDomoManager.Instance.OnEntitiesPublished    += delegate (List<EntityData> _l) { CheckEntity(); };
			MajorDomoManager.Instance.OnEntitiesRevoked      += delegate (List<EntityData> _l) { CheckEntity(); };
			MajorDomoManager.Instance.OnEntityControlChanged += delegate (List<EntityData> _l) { CheckEntity(); };

			if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Server)
			{
				if (CanDisableGameObject()) gameObject.SetActive(false);
			}
		}


		public void Update()
		{
			if ((Entity != null) && (Entity.State == EntityData.EntityState.Registered))
			{
				if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Server)
				{
					if (Entity.IsUpdated())
					{
						SynchroniseFromEntity();
						Entity.ResetUpdated();
						// counteract callbacks from changing values
						ResetModified();
					}
				}
				else if (IsModified())
				{
					if (IsControlledByClient())
					{
						SynchroniseToEntity();
						ResetModified();
					}
					else if (SharedControl)
					{
						// local object was modified, but client is not yet in control > request
						MajorDomoManager.Instance.RequestControl(Entity);
					}
				}
			}
		}


		public void OnDisable()
		{
			// make sure last state is synchronised
			if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Client)
			{
				SynchroniseToEntity();
			}
		}


		public void OnEnable()
		{
			// make sure current state is synchronised
			if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Client)
			{
				SynchroniseToEntity();
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

					Persistent    = Entity.IsPersistent();
					SharedControl = Entity.AllowsSharedControl();

					if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Server)
					{
						// server authority > update immediately
						SynchroniseFromEntity();
					}
					else
					{
						// if this entity is persistent, get an update from the server first
						if (Persistent)
						{
							SynchroniseFromEntity();
						}
					}

					Entity.OnEntityUpdated += Update;

					Debug.LogFormat("{0} '{1}' synchronised with entity '{2}'",
						GetEntityTypeName(), name, Entity.ToString(true, true));
				}
				else
				{
					// entity not found. let's create and publish it
					if ((SynchronisationMode == MajorDomoManager.SynchronisationMode.Client) && MajorDomoManager.Instance.IsConnected())
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
					GetEntityTypeName(), name, Entity.ToString(true, false));

				DestroyVariables();

				if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Server)
				{
					if (CanDisableGameObject()) gameObject.SetActive(false);
				}

				Entity = null;
			}
			else
			{
				// control might have changed. check for that

				if (IsControlledByClient())
				{
					// this entity is now controlled by this client
					SynchronisationMode = MajorDomoManager.SynchronisationMode.Client;
				}
				else
				{
					SynchronisationMode = MajorDomoManager.SynchronisationMode.Server;
				}
			}
		}


		public bool IsControlledByClient()
		{
			return (Entity != null) && Entity.IsControlledByClient(MajorDomoManager.Instance.ClientUID);
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

		protected EntityData Entity { get; private set; }
	}
}
