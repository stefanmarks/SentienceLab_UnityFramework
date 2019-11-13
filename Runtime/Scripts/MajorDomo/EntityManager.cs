#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System.Collections.Generic;
using UnityEngine;

namespace SentienceLab.MajorDomo
{
	public class EntityManager
	{

		public EntityManager()
		{
			m_entityUidMap     = new SortedList<uint, EntityData>();
			m_clientUID        = ClientData.UID_UNASSIGNED;
			m_clientEntityList = new List<EntityData>();
			m_modifiedEntities = new List<EntityData>();
			Reset();
		}


		public void SetClientUID(uint _clientUID)
		{
			m_clientUID = _clientUID;
			m_rebuildClientEntityList = true;
		}


		public void Reset()
		{
			while (m_entityUidMap.Count > 0)
			{
				EntityData entity = m_entityUidMap.Values[0];
				RemoveEntity(entity);
				entity.SetRevoked();
			}
			m_rebuildClientEntityList = true;
		}


		public EntityData CreateClientEntity(string _name)
		{
			EntityData entity = FindEntity(_name);
			if (entity != null)
			{
				// name is already registered
				if ((entity.ClientUID != ClientData.UID_SERVER) && (entity.ClientUID != m_clientUID))
				{
					// but to some other client. sorry...
					entity = null;
				}
			}
			else
			{
				entity = new EntityData(_name);
			}

			if (entity != null)
			{
				entity.SetClientUID(m_clientUID);
			}

			return entity;
		}


		public EntityData AddEntity(EntityData _entity)
		{
			EntityData registeredEntity = null;
		
			if (m_entityUidMap.TryGetValue(_entity.EntityUID, out registeredEntity))
			{
				if (!_entity.Equals(registeredEntity))
				{
					Debug.LogWarning("Entity mismatch " + _entity.ToString(false, false) + " <-> " + registeredEntity.ToString(false, false));
				}
			}
			else
			{
				registeredEntity = _entity;
				m_entityUidMap[_entity.EntityUID] = _entity;
				Debug.Log("Added entity " + _entity.ToString(true, true));
				m_rebuildClientEntityList = true;
			}
			return registeredEntity;
		}


		public EntityData UpdateEntity(AUT_WH.MajorDomoProtocol.EntityUpdate _update)
		{
			EntityData entity = null;
			if (m_entityUidMap.TryGetValue(_update.EntityUID, out entity))
			{
				// do not update own clients' entities
				if (entity.ClientUID != m_clientUID)
				{
					entity.ReadEntityUpdate(_update);
				}
			}
			else
			{
				Debug.LogWarning("Trying to update invalid server entity with UID = " + _update.EntityUID);
			}
			return entity;
		}


		public EntityData FindEntity(uint _uid)
		{
			EntityData entity = null;
			m_entityUidMap.TryGetValue(_uid, out entity);
			return entity;
		}


		public EntityData FindEntity(string _name)
		{
			EntityData entity = null;
			foreach (EntityData e in m_entityUidMap.Values)
			{
				if (e.Name == _name)
				{
					entity = e;
					break;
				}
			}
			return entity;
		}


		public List<EntityData> GetClientEntities()
		{
			if (m_rebuildClientEntityList)
			{
				m_clientEntityList.Clear();
				foreach (EntityData entity in m_entityUidMap.Values)
				{
					if (entity.ClientUID == m_clientUID)
					{
						m_clientEntityList.Add(entity);
					}
				}
				m_rebuildClientEntityList = false;
			}
			return m_clientEntityList;
		}


		public List<EntityData> GetModifiedEntities()
		{
			m_modifiedEntities.Clear();
			foreach (var entity in GetClientEntities())
			{
				if (entity.IsModified()) m_modifiedEntities.Add(entity);
			}
			return m_modifiedEntities;
		}


		public void ResetModifiedEntities()
		{
			foreach(var entity in m_modifiedEntities)
			{
				entity.ResetModified();
			}
			m_modifiedEntities.Clear();
		}


		public void RemoveEntity(EntityData _entity)
		{
			if (m_entityUidMap.Remove(_entity.EntityUID))
			{
				Debug.Log("Removed entity " + _entity.ToString(true, false));
				m_clientEntityList.Remove(_entity); // remove from this list as well (if existing)
			}
			else
			{
				Debug.LogWarning("Trying to remove unknown entity with UID=" + _entity.EntityUID);
			}
		}


		public bool ChangeEntityControl(EntityData _entity, uint _clientUID)
		{
			bool controlChanged = _entity.ChangeClientUID(_clientUID);
			if (controlChanged) m_rebuildClientEntityList = true;
			return controlChanged;
		}


		private uint                         m_clientUID;
		private SortedList<uint, EntityData> m_entityUidMap;
		private readonly List<EntityData>    m_clientEntityList;
		private bool                         m_rebuildClientEntityList;
		private readonly List<EntityData>    m_modifiedEntities;
	}
}
