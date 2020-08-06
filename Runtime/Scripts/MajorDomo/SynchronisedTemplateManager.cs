#region Copyright Information
// Sentience Lab MajorDomo Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand
// (C) Westfälische Hochschule, Gelsenkirchen, Germany
#endregion Copyright Information

using UnityEngine;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SentienceLab.MajorDomo
{
	[AddComponentMenu("MajorDomo/Synchronised Template Manager")]
	public class SynchronisedTemplateManager : MonoBehaviour
	{
		[System.Serializable]
		public struct TemplateEntry
		{
			public string     Name;
			public GameObject Template;
		}

		[Tooltip("Templates to listen for and spawn when registered with the MajorDomo manager")]
		public List<TemplateEntry> Templates;

		[Tooltip("Naming pattern for spawned templates ({0} is replaced with the entity name)")]
		public string NamingPattern = "{0}";


		public void Awake()
		{
			if (MajorDomoManager.Instance == null)
			{
				Debug.LogWarning("MajorDomoManager component needed");
				this.enabled = false;
				return;
			}

			m_publishedEntities = new Queue<EntityData>();
			m_revokedEntities   = new Queue<EntityData>();
			m_spawnedTemplates  = new Dictionary<string, GameObject>();

			MajorDomoManager.Instance.OnEntitiesPublished += OnEntitiesPublished;
			MajorDomoManager.Instance.OnEntitiesRevoked   += OnEntitiesRevoked;
		}


		private void OnEntitiesPublished(List<EntityData> _entities)
		{
			foreach (var e in _entities) m_publishedEntities.Enqueue(e);
		}


		protected void CheckPublishedEntity(EntityData _entity)
		{
			var templateName = _entity.GetValue_String("template");
			if (templateName != null)
			{
				Debug.LogFormat("Published entity '{0}' with template '{1}'", _entity.Name, templateName.Value);
				foreach (var t in Templates)
				{
					string name = t.Name;
					if (name.Equals(templateName.Value))
					{
						string newName = string.Format(NamingPattern, _entity.Name);
						Debug.LogFormat("Spawning template '{0}' as '{1}'", name, newName);
						GameObject go = Instantiate(t.Template, this.transform);
						go.name = newName;
						SynchronisedGameObject[] sgo = go.GetComponentsInChildren<SynchronisedGameObject>();
						foreach (var s in sgo)
						{
							// make sure name template does not affect the entity name
							if (s.EntityName.Equals(SynchronisedGameObject.GAMEOBJECT_AUTO_NAME))
							{
								s.EntityName = _entity.Name;
							}
							// adapt reference transform if necessary
							if ((s.TargetTransform != null) && (s.ReferenceTransform == null))
							{
								s.ReferenceTransform = this.transform;
							}
						}
						m_spawnedTemplates[_entity.Name] = go;
					}
				}
			}
		}


		private void OnEntitiesRevoked(List<EntityData> _entities)
		{
			foreach (var e in _entities) m_revokedEntities.Enqueue(e);
		}


		protected void CheckRevokedEntity(EntityData _entity)
		{
			var name = _entity.Name;
			if (m_spawnedTemplates.TryGetValue(name, out var go))
			{
				Debug.LogFormat("Revoked templated entity '{0}' > Destroying GameObject '{1}'", name, go.name);
				m_spawnedTemplates.Remove(name);
				Destroy(go);
			}
		}


		public void Update()
		{
			// check entities one by one to avoid spawn "bottlenecks"
			if (m_publishedEntities.Count > 0) CheckPublishedEntity(m_publishedEntities.Dequeue());
			if (m_revokedEntities.Count   > 0) CheckRevokedEntity(  m_revokedEntities.Dequeue()  );

		}


		private Dictionary<string, GameObject> m_spawnedTemplates;
		private Queue<EntityData>              m_publishedEntities, m_revokedEntities;
	}
}
