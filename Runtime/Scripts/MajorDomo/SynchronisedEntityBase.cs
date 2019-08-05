#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;

namespace SentienceLab.MajorDomo
{
	public abstract class SynchronisedEntityBase : MonoBehaviour
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

			// empty name > use game object name
			if (EntityName.Trim().Length == 0)
			{
				EntityName = gameObject.name;
			}
			// replace template string
			EntityName = EntityName.Replace("{GAMEOBJECT}", this.gameObject.name);

			m_entity = null;

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
			if ((m_entity != null) && (m_entity.State == EntityData.EntityState.Registered))
			{
				if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Server)
				{
					if (m_entity.IsUpdated())
					{
						SynchroniseFromEntity();
						m_entity.ResetUpdated();
					}
				}
				else
				{
					SynchroniseToEntity();
				}
			}
		}


		public void OnDisable()
		{
			if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Client)
			{
				SynchroniseToEntity();
			}
		}


		public void OnEnable()
		{
			if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Client)
			{
				SynchroniseToEntity();
			}
		}


		protected void CheckEntity()
		{
			if (m_entity == null)
			{
				m_entity = MajorDomoManager.Instance.FindEntity(EntityName);
				if (m_entity != null)
				{
					FindVariables();

					Persistent    = m_entity.IsPersistent();
					SharedControl = m_entity.AllowsSharedControl();

					if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Server)
					{
						m_entity.OnEntityUpdated += Update;
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

					Debug.LogFormat("{0} '{1}' synchronised with entity '{2}'", m_entityType, name, m_entity.ToString(true, true));
				}
				else
				{
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
							Debug.LogFormat("Publishing {0} entity '{1}'", m_entityType, entity.ToString(true, true));
						}
					}
				}
			}
			else if (m_entity.State == EntityData.EntityState.Revoked)
			{
				Debug.LogFormat("{0} '{1}' lost synchronisation with entity '{2}'", m_entityType, name, m_entity.ToString(true, false));

				DestroyVariables();

				if (SynchronisationMode == MajorDomoManager.SynchronisationMode.Server)
				{
					if (CanDisableGameObject()) gameObject.SetActive(false);
				}

				m_entity = null;
			}
			else
			{
				if (m_entity.ClientUID == MajorDomoManager.Instance.ClientUID)
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


		protected abstract void CreateVariables(EntityData _entity);

		protected abstract void FindVariables();

		protected abstract void DestroyVariables();

		protected abstract void SynchroniseFromEntity();

		protected abstract void SynchroniseToEntity();

		protected abstract bool CanDisableGameObject();


		protected EntityData m_entity;
		protected string     m_entityType;
	}
}
