using UnityEngine;

namespace SentienceLab
{
	/// <summary>
	/// Script that spawns multiple GameObjects in a volume.
	/// </summary>
	/// 
	[AddComponentMenu("SentienceLab/Tools/Volume Spawner")]

	public class VolumeSpawner : MonoBehaviour
	{
		[Tooltip("Prefab of the object to spawn")]
		public GameObject prefab;

		[Tooltip("Size of the volume to spawn the prefab in")]
		public Bounds spawnVolume;

		[Tooltip("Amount of spawned elements along the X-axis")]
		public int elementsX = 10;

		[Tooltip("Amount of spawned elements along the Y-axis")]
		public int elementsY = 1;

		[Tooltip("Amount of spawned elements along the Z-axis")]
		public int elementsZ = 10;

	
		public void Start () 
		{
			Vector3 offset = Vector3.zero;
			for (int x = 0; x < elementsX; x++)
				for (int y = 0; y < elementsY; y++)
					for (int z = 0; z < elementsZ; z++)
					{
						GameObject o = Instantiate(prefab, this.transform);
						o.name = prefab.name + "_" + x + "/" + y + "/" + z;
						offset.x = Mathf.Lerp(spawnVolume.min.x, spawnVolume.max.x, (x + 0.5f) / elementsX);
						offset.y = Mathf.Lerp(spawnVolume.min.y, spawnVolume.max.y, (y + 0.5f) / elementsY);
						offset.z = Mathf.Lerp(spawnVolume.min.z, spawnVolume.max.z, (z + 0.5f) / elementsZ);
						o.transform.SetLocalPositionAndRotation(offset, Quaternion.identity);
						o.SetActive(true);
					}
		}
	}
}