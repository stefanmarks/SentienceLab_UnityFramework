#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SentienceLab.MajorDomo
{
	public class EntityManager
	{

		public EntityManager()
		{
			m_entityUidMap     = new SortedList<uint, EntityData>();
			m_clientEntityList = new List<EntityData>();
			Reset();
		}


		public void SetClientUID(uint _clientUID)
		{
			m_clientUID = _clientUID;
			m_rebuildClientEntityList = true;
		}


		public void Reset()
		{
			m_clientUID = ClientData.UID_UNASSIGNED;
			m_entityUidMap.Clear();
			m_clientEntityList.Clear();
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
			if (m_entityUidMap.TryGetValue(_entity.EntityUID, out EntityData registeredEntity))
			{
				if (!_entity.Equals(registeredEntity))
				{
					Debug.LogWarningFormat("Entity mismatch {0} <-> {1}",
						_entity.ToString(false, false),
						registeredEntity.ToString(false, false));
				}
			}
			else
			{
				registeredEntity = _entity;
				m_entityUidMap[_entity.EntityUID] = _entity;
				Debug.LogFormat("Added entity {0}", _entity.ToString(true, true));
				m_rebuildClientEntityList = true;
			}
			return registeredEntity;
		}


		public EntityData UpdateEntity(AUT_WH.MajorDomoProtocol.EntityUpdate _update)
		{
			if (m_entityUidMap.TryGetValue(_update.EntityUID, out EntityData entity))
			{
				// do not update own clients' entities
				if (entity.ClientUID != m_clientUID)
				{
					entity.ReadEntityUpdate(_update);
				}
			}
			else
			{
				Debug.LogWarningFormat(
					"Trying to update invalid server entity with UID={0}",
					_update.EntityUID);
			}
			return entity;
		}


		public IReadOnlyList<EntityData> GetEntities()
		{
			return new List<EntityData>(m_entityUidMap.Values).AsReadOnly();
		}


		public EntityData FindEntity(uint _uid)
		{
			m_entityUidMap.TryGetValue(_uid, out EntityData entity);
			return entity;
		}


		public EntityData FindEntity(string _name)
		{
			EntityData entity = null;
			foreach (EntityData e in m_entityUidMap.Values)
			{
				if (e.Name.Equals(_name))
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


		public void GetModifiedClientEntities(ref List<EntityData> _modifiedEntities)
		{
			foreach (var entity in GetClientEntities())
			{
				if (entity.IsModified()) _modifiedEntities.Add(entity);
			}
		}


		public void ResetModifiedEntities(ref List<EntityData> _modifiedEntities)
		{
			foreach(var entity in _modifiedEntities)
			{
				entity.ResetModified();
			}
		}


		public void RemoveEntity(EntityData _entity)
		{
			if (m_entityUidMap.Remove(_entity.EntityUID))
			{
				Debug.LogFormat("Removed entity {0}", _entity.ToString(true, false));
				m_clientEntityList.Remove(_entity); // remove from this list as well (if existing)
			}
			else
			{
				Debug.LogWarningFormat(
					"Trying to remove unknown entity with UID={0}",
					_entity.EntityUID);
			}
		}


		public bool ChangeEntityControl(EntityData _entity, uint _clientUID)
		{
			bool controlChanged = _entity.ChangeClientUID(_clientUID);
			if (controlChanged) m_rebuildClientEntityList = true;
			return controlChanged;
		}


		public static string EntityListAsString(IReadOnlyList<EntityData> _entityList)
		{
			StringBuilder sb = new StringBuilder();
			int idx = 1;
			foreach(var entity in _entityList)
			{
				if (idx > 1) sb.Append('\n');
				sb.Append(idx).Append(":\t").Append(entity.ToString(true, true));
				idx++;
			}
			return sb.ToString();
		}


		private uint                         m_clientUID;
		private SortedList<uint, EntityData> m_entityUidMap;
		private readonly List<EntityData>    m_clientEntityList;
		private bool                         m_rebuildClientEntityList;
	}
}
