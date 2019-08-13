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
			entityUidMap = null;
			Reset();
		}


		public void Reset()
		{
			if (entityUidMap == null)
			{
				entityUidMap = new SortedList<uint, EntityData>();
			}
			else
			{
				while (entityUidMap.Count > 0)
				{
					EntityData entity = entityUidMap.Values[0];
					RemoveEntity(entity);
					entity.SetRevoked();
				}
			}
		}


		public EntityData CreateClientEntity(string _name, uint _clientUID)
		{
			EntityData entity = FindEntity(_name);
			if (entity != null)
			{
				// name is already registered
				if ((entity.ClientUID != ClientData.UID_SERVER) && (entity.ClientUID != _clientUID))
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
				entity.SetClientUID(_clientUID);
			}
			return entity;
		}


		public EntityData AddEntity(EntityData _entity)
		{
			EntityData registeredEntity = null;
		
			if (entityUidMap.TryGetValue(_entity.EntityUID, out registeredEntity))
			{
				if (!_entity.Equals(registeredEntity))
				{
					Debug.LogWarning("Entity mismatch " + _entity.ToString(false, false) + " <-> " + registeredEntity.ToString(false, false));
				}
			}
			else
			{
				registeredEntity = _entity;
				entityUidMap[_entity.EntityUID] = _entity;
				Debug.Log("Added entity " + _entity.ToString(true, true));
			}
			return registeredEntity;
		}


		public void UpdateEntity(AUT_WH.MajorDomoProtocol.EntityUpdate _update, uint _localClientUID)
		{
			EntityData entity = null;
			if (entityUidMap.TryGetValue(_update.EntityUID, out entity))
			{
				// do not update own clients' entities
				if (entity.ClientUID != _localClientUID)
				{
					entity.ReadEntityUpdate(_update);
				}
			}
			else
			{
				Debug.LogWarning("Trying to update invalid server entity with UID = " + _update.EntityUID);
			}
		}


		public EntityData FindEntity(uint _uid)
		{
			EntityData entity = null;
			entityUidMap.TryGetValue(_uid, out entity);
			return entity;
		}


		public EntityData FindEntity(string _name)
		{
			EntityData entity = null;
			foreach (EntityData e in entityUidMap.Values)
			{
				if (e.Name == _name)
				{
					entity = e;
					break;
				}
			}
			return entity;
		}


		public List<EntityData> GetClientEntities(uint _clientUID)
		{
			List<EntityData> clientEntities = new List<EntityData>();
			foreach (EntityData entity in entityUidMap.Values)
			{
				if (entity.ClientUID == _clientUID)
				{
					clientEntities.Add(entity);
				}
			}
			return clientEntities;
		}


		public void RemoveEntity(EntityData _entity)
		{
			if (entityUidMap.Remove(_entity.EntityUID))
			{
				Debug.Log("Removed entity " + _entity.ToString(true, false));
			}
			else
			{
				Debug.LogWarning("Trying to remove unknown entity with UID=" + _entity.EntityUID);
			}
		}


		private SortedList<uint, EntityData> entityUidMap;
	}
}
