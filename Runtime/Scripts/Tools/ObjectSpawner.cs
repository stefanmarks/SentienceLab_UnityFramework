#region Copyright Information
// SentienceLab Unity Framework
// (C) SentienceLab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using UnityEngine;
using UnityEngine.Events;

namespace SentienceLab
{
	/// <summary>
	/// Component for spawning prefabs.
	/// </summary>
	[AddComponentMenu("SentienceLab/Tools/Object Spawner")]
	public class ObjectSpawner : MonoBehaviour
	{
		[Tooltip("Prefab to spawn")]
		public GameObject spawnPrefab;

		[Tooltip("Name template for the spawned objects ({0}: template name, {1}: spawn counter)")]
		public string spawnedObjectName = "{0}_{1}";

		[Tooltip("Parent of the spawned object (null: World)")]
		public Transform spawnedObjectParent = null;

		[Tooltip("Start velocity of the spawned object (requires rigidbody)")]
		public Vector3 startVelocity = Vector3.zero;

		[Tooltip("Maximum amount of objects to spawn in total (0: unlimited)")]
		public int maxSpawnCount = 0;

		[Tooltip("Minimum amount of time between spawns")]
		public float minSpawnInterval = 0;

		[System.Serializable]
		public class Events
		{
			[Tooltip("Event fired when the an object was spawned")]
			public UnityEvent<GameObject> OnObjectSpawned;

			[Tooltip("Event fired when the next object can be spawned")]
			public UnityEvent OnNextSpawnPossible;
		}

		public Events events;


		public void Start()
		{
			m_noSpawnTimer = 0;
		}


		public void Update()
		{
			// does the counter still allow spawning?
			if ((maxSpawnCount <= 0) || (m_spawnCounter < maxSpawnCount))
			{
				// handle spawn timer
				if (m_noSpawnTimer > 0)
				{
					m_noSpawnTimer = Mathf.Max(0, m_noSpawnTimer - Time.deltaTime);
					if (m_noSpawnTimer == 0)
					{
						events.OnNextSpawnPossible.Invoke();
					}
				}
			}
		}


		public void SpawnObject()
		{
			if (m_noSpawnTimer <= 0)
			{
				var newObject = Instantiate(spawnPrefab, this.transform);
				newObject.transform.parent = spawnedObjectParent;
				newObject.name = string.Format(spawnedObjectName, spawnPrefab.name, m_spawnCounter);

				Rigidbody rb = newObject.GetComponentInChildren<Rigidbody>();
				if (rb != null)
				{
					rb.velocity = this.transform.TransformVector(startVelocity);
				}
				
				m_spawnCounter++;
				m_noSpawnTimer = Mathf.Max(Time.deltaTime, minSpawnInterval);
				events.OnObjectSpawned.Invoke(newObject);
			}
		}


		protected float m_noSpawnTimer;
		protected int   m_spawnCounter;
	}
}